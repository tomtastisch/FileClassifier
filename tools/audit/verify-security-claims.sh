#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
CHECK_ID="security-claims-evidence"
OUT_DIR_REL="artifacts/ci/${CHECK_ID}"
OUT_DIR="${ROOT_DIR}/${OUT_DIR_REL}"
RAW_LOG="${OUT_DIR}/raw.log"
SUMMARY_MD="${OUT_DIR}/summary.md"
RESULT_JSON="${OUT_DIR}/result.json"

mkdir -p "${OUT_DIR}"
: > "${RAW_LOG}"

STARTED_AT="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
START_EPOCH="$(date -u +%s)"

VIOLATIONS_JSONL="${OUT_DIR}/.violations.jsonl"
: > "${VIOLATIONS_JSONL}"

PASS_COUNT=0
WARN_COUNT=0
FAIL_COUNT=0

log() {
  printf '%s\n' "$*" | tee -a "${RAW_LOG}" >/dev/null
}

add_violation() {
  local rule_id="$1"
  local severity="$2"
  local message="$3"
  local evidence="$4"

  if [[ "${severity}" == "warn" ]]; then
    WARN_COUNT=$((WARN_COUNT + 1))
  else
    FAIL_COUNT=$((FAIL_COUNT + 1))
  fi

  python3 - "$rule_id" "$severity" "$message" "$evidence" >> "${VIOLATIONS_JSONL}" <<'PY'
import json,sys
rule_id,severity,message,evidence=sys.argv[1:5]
print(json.dumps({
  "rule_id": rule_id,
  "severity": severity,
  "message": message,
  "evidence_paths": [evidence],
}, ensure_ascii=True))
PY
}

add_pass() {
  PASS_COUNT=$((PASS_COUNT + 1))
}

run_cmd_capture() {
  local label="$1"
  shift
  log "# ${label}"
  log "$ $*"
  if "$@" >> "${RAW_LOG}" 2>&1; then
    return 0
  fi
  return 1
}

retry_gh_api() {
  local endpoint="$1"
  local out_file="$2"
  local attempts=3
  local delay=2
  local i

  for i in $(seq 1 "${attempts}"); do
    if gh api "${endpoint}" > "${out_file}" 2>> "${RAW_LOG}"; then
      return 0
    fi
    if [[ "${i}" -lt "${attempts}" ]]; then
      sleep "${delay}"
      delay=$((delay * 2))
    fi
  done
  return 1
}

require_tool() {
  local t="$1"
  if ! command -v "${t}" >/dev/null 2>&1; then
    add_violation "CI-SEC-CLAIM-000" "fail" "Missing required tool '${t}'" "tools/audit/verify-security-claims.sh"
    return 1
  fi
  return 0
}

require_tool gh || true
require_tool jq || true

has_rg() {
  command -v rg >/dev/null 2>&1
}

match_file() {
  local pattern="$1"
  local file="$2"
  if has_rg; then
    rg -q "$pattern" "$file"
  else
    grep -Eq "$pattern" "$file"
  fi
}

REPO_FULL="${GITHUB_REPOSITORY:-}"
if [[ -z "${REPO_FULL}" ]]; then
  origin_url="$(git -C "${ROOT_DIR}" remote get-url origin 2>/dev/null || true)"
  if [[ "${origin_url}" =~ github.com[:/]([^/]+/[^/.]+)(\.git)?$ ]]; then
    REPO_FULL="${BASH_REMATCH[1]}"
  fi
fi

if [[ -z "${REPO_FULL}" ]]; then
  add_violation "CI-SEC-CLAIM-001" "fail" "Unable to determine GitHub repository slug" "SECURITY.md"
fi

# Claim: 5.x supported and <5.0 unsupported maps to current package major = 5
pkg_ver="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "${ROOT_DIR}/src/FileTypeDetection/FileTypeDetectionLib.vbproj" | head -n1)"
if [[ -z "${pkg_ver}" ]]; then
  add_violation "CI-SEC-CLAIM-002" "fail" "Package version not found" "src/FileTypeDetection/FileTypeDetectionLib.vbproj"
else
  major="${pkg_ver%%.*}"
  if [[ "${major}" == "5" ]]; then
    add_pass
  else
    add_violation "CI-SEC-CLAIM-002" "fail" "Expected package major 5 for SECURITY.md support claim, found ${pkg_ver}" "src/FileTypeDetection/FileTypeDetectionLib.vbproj"
  fi
fi

# Claim: security-nuget gate exists in CI
if match_file "^[[:space:]]+security-nuget:" "${ROOT_DIR}/.github/workflows/ci.yml" && match_file "run\\.sh security-nuget" "${ROOT_DIR}/.github/workflows/ci.yml"; then
  add_pass
else
  add_violation "CI-SEC-CLAIM-003" "fail" "security-nuget gate missing from CI workflow" ".github/workflows/ci.yml"
fi

# Claim: OIDC trusted publishing present in release workflow
if match_file "NuGet/login@v1" "${ROOT_DIR}/.github/workflows/release.yml" && match_file "assert OIDC temp key present" "${ROOT_DIR}/.github/workflows/release.yml"; then
  add_pass
else
  add_violation "CI-SEC-CLAIM-004" "fail" "OIDC trusted publishing markers missing" ".github/workflows/release.yml"
fi

if [[ -n "${REPO_FULL}" ]]; then
  tmp_repo_json="${OUT_DIR}/.repo.json"
  tmp_pvr_json="${OUT_DIR}/.pvr.json"
  tmp_branch_json="${OUT_DIR}/.branch-protection.json"

  if retry_gh_api "repos/${REPO_FULL}" "${tmp_repo_json}"; then
    dep_status="$(jq -r '.security_and_analysis.dependabot_security_updates.status // "unknown"' "${tmp_repo_json}")"
    sec_status="$(jq -r '.security_and_analysis.secret_scanning.status // "unknown"' "${tmp_repo_json}")"
    default_branch="$(jq -r '.default_branch' "${tmp_repo_json}")"

    if [[ "${dep_status}" == "enabled" ]]; then
      add_pass
    else
      add_violation "CI-SEC-CLAIM-005" "fail" "Dependabot security updates expected enabled, got ${dep_status}" "${OUT_DIR_REL}/raw.log"
    fi

    if [[ "${sec_status}" == "enabled" ]]; then
      add_pass
    else
      add_violation "CI-SEC-CLAIM-006" "fail" "Secret scanning expected enabled, got ${sec_status}" "${OUT_DIR_REL}/raw.log"
    fi

    if retry_gh_api "repos/${REPO_FULL}/private-vulnerability-reporting" "${tmp_pvr_json}"; then
      pvr_enabled="$(jq -r '.enabled // false' "${tmp_pvr_json}")"
      if [[ "${pvr_enabled}" == "true" ]]; then
        add_pass
      else
        add_violation "CI-SEC-CLAIM-007" "fail" "Private vulnerability reporting expected true" "${OUT_DIR_REL}/raw.log"
      fi
    else
      add_violation "CI-SEC-CLAIM-007" "fail" "GitHub API failed for private-vulnerability-reporting after retries" "${OUT_DIR_REL}/raw.log"
    fi

    if retry_gh_api "repos/${REPO_FULL}/branches/${default_branch}/protection" "${tmp_branch_json}"; then
      required_contexts=("preflight" "version-policy" "build" "api-contract" "pack" "consumer-smoke" "package-backed-tests" "security-nuget" "tests-bdd-coverage")
      missing=0
      for ctx in "${required_contexts[@]}"; do
        if ! jq -e --arg ctx "${ctx}" '.required_status_checks.contexts | index($ctx)' "${tmp_branch_json}" >/dev/null; then
          log "Missing branch protection context: ${ctx}"
          missing=1
        fi
      done
      if [[ "${missing}" -eq 0 ]]; then
        add_pass
      else
        add_violation "CI-SEC-CLAIM-008" "fail" "Branch protection contexts do not match SECURITY evidence baseline" "${OUT_DIR_REL}/raw.log"
      fi
    else
      add_violation "CI-SEC-CLAIM-008" "fail" "GitHub API failed for branch protection after retries" "${OUT_DIR_REL}/raw.log"
    fi
  else
    add_violation "CI-SEC-CLAIM-005" "fail" "GitHub API failed for repository metadata after retries" "${OUT_DIR_REL}/raw.log"
    add_violation "CI-SEC-CLAIM-006" "fail" "GitHub API failed for repository metadata after retries" "${OUT_DIR_REL}/raw.log"
    add_violation "CI-SEC-CLAIM-007" "fail" "GitHub API failed for repository metadata after retries" "${OUT_DIR_REL}/raw.log"
    add_violation "CI-SEC-CLAIM-008" "fail" "GitHub API failed for repository metadata after retries" "${OUT_DIR_REL}/raw.log"
  fi
fi

if [[ "${FAIL_COUNT}" -eq 0 ]]; then
  STATUS="pass"
else
  STATUS="fail"
fi

FINISHED_AT="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
END_EPOCH="$(date -u +%s)"
DURATION_MS="$(( (END_EPOCH - START_EPOCH) * 1000 ))"

{
  echo "# Security Claims Evidence"
  echo
  echo "- status: ${STATUS}"
  echo "- pass_count: ${PASS_COUNT}"
  echo "- warn_count: ${WARN_COUNT}"
  echo "- fail_count: ${FAIL_COUNT}"
  echo "- started_at: ${STARTED_AT}"
  echo "- finished_at: ${FINISHED_AT}"
  echo
  echo "Evidence root: ${OUT_DIR_REL}"
} > "${SUMMARY_MD}"

python3 - "${VIOLATIONS_JSONL}" "${RESULT_JSON}" "${STATUS}" "${STARTED_AT}" "${FINISHED_AT}" "${DURATION_MS}" "${OUT_DIR_REL}" <<'PY'
import json
import pathlib
import sys

viol_path, result_path, status, started_at, finished_at, duration_ms, out_rel = sys.argv[1:8]
violations = []
vp = pathlib.Path(viol_path)
if vp.exists():
    for line in vp.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line:
            violations.append(json.loads(line))

result = {
    "schema_version": 1,
    "check_id": "security-claims-evidence",
    "status": status,
    "rule_violations": violations,
    "evidence_paths": [f"{out_rel}/raw.log", f"{out_rel}/summary.md"],
    "artifacts": [
        f"{out_rel}/raw.log",
        f"{out_rel}/summary.md",
        f"{out_rel}/result.json",
    ],
    "timing": {
        "started_at": started_at,
        "finished_at": finished_at,
        "duration_ms": int(duration_ms),
    },
}
pathlib.Path(result_path).write_text(json.dumps(result, ensure_ascii=True), encoding="utf-8")
PY

if [[ "${STATUS}" != "pass" ]]; then
  exit 1
fi
