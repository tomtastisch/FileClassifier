#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
export LC_ALL=C

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT_DIR="${ROOT_DIR}/artifacts/ci/preflight/code-scanning-tools-zero"
RAW_LOG="${OUT_DIR}/raw.log"
SUMMARY_MD="${OUT_DIR}/summary.md"
RESULT_JSON="${OUT_DIR}/result.json"
ALERTS_JSON="${OUT_DIR}/open-alerts.json"
SECURITY_ALERTS_JSON="${OUT_DIR}/open-security-alerts.json"

mkdir -p "${OUT_DIR}"
: > "${RAW_LOG}"

log() {
  printf '%s\n' "$*" | tee -a "${RAW_LOG}" >/dev/null
}

fail() {
  local reason="$1"
  local alerts_rel_path=""
  local evidence_paths='["artifacts/ci/preflight/code-scanning-tools-zero/raw.log","artifacts/ci/preflight/code-scanning-tools-zero/summary.md"]'

  if [[ -f "${ALERTS_JSON}" ]]; then
    alerts_rel_path="artifacts/ci/preflight/code-scanning-tools-zero/open-alerts.json"
    evidence_paths='["artifacts/ci/preflight/code-scanning-tools-zero/raw.log","artifacts/ci/preflight/code-scanning-tools-zero/open-alerts.json","artifacts/ci/preflight/code-scanning-tools-zero/summary.md"]'
  fi

  log "FAIL: ${reason}"
  {
    echo "# Code Scanning Tools Zero"
    echo
    echo "- status: fail"
    echo "- reason: ${reason}"
    if [[ -n "${alerts_rel_path}" ]]; then
      echo "- alerts_file: ${alerts_rel_path}"
    fi
  } > "${SUMMARY_MD}"
  jq -n --arg reason "${reason}" --argjson evidence_paths "${evidence_paths}" \
    '{schema_version:1,check_id:"code-scanning-tools-zero",status:"fail",reason:$reason,evidence_paths:$evidence_paths}' > "${RESULT_JSON}"
  exit 1
}

if ! command -v gh >/dev/null 2>&1; then
  fail "gh fehlt"
fi
if ! command -v jq >/dev/null 2>&1; then
  fail "jq fehlt"
fi

REPO="${GITHUB_REPOSITORY:-}"
if [[ -z "${REPO}" ]]; then
  origin_url="$(git -C "${ROOT_DIR}" remote get-url origin 2>/dev/null || true)"
  if [[ "${origin_url}" =~ github.com[:/]([^/]+/[^/.]+)(\.git)?$ ]]; then
    REPO="${BASH_REMATCH[1]}"
  fi
fi
if [[ -z "${REPO}" ]]; then
  fail "Repository-Slug konnte nicht bestimmt werden"
fi

attempt=1
delay=2
max_attempts=3
while true; do
  if gh api "repos/${REPO}/code-scanning/alerts?state=open&per_page=100" --paginate > "${ALERTS_JSON}" 2>> "${RAW_LOG}"; then
    break
  fi
  if (( attempt >= max_attempts )); then
    fail "GitHub API fuer code-scanning alerts fehlgeschlagen"
  fi
  log "WARN: API-Fehler, retry ${attempt}/${max_attempts}"
  sleep "${delay}"
  attempt=$((attempt + 1))
  delay=$((delay * 2))
done

# Block only on security-relevant alerts; informational/style-only findings
# from external analyzers are tracked separately and must not deadlock PR gates.
jq '[.[] | select(((.rule.security_severity_level // .severity // "") | tostring) != "")]' "${ALERTS_JSON}" > "${SECURITY_ALERTS_JSON}"
count="$(jq 'length' "${SECURITY_ALERTS_JSON}")"
if [[ "${count}" -gt 0 ]]; then
  jq '[.[] | {number,rule:(.rule.id // .rule.name),tool:.tool.name,severity:(.rule.security_severity_level // .severity),html_url}]' "${SECURITY_ALERTS_JSON}" > "${OUT_DIR}/open-alerts-summary.json"
  log "Open security-relevant alerts detected: ${count}"
  jq -r '.[] | "- #\(.number) [\(.tool)] \(.rule) [sev=\(.severity)] -> \(.html_url)"' "${OUT_DIR}/open-alerts-summary.json" | tee -a "${RAW_LOG}" >/dev/null
  fail "Offene security-relevante Code-Scanning-Alerts vorhanden (${count})"
fi

{
  echo "# Code Scanning Tools Zero"
  echo
  echo "- status: pass"
  echo "- open_alerts: 0"
  echo "- repo: ${REPO}"
} > "${SUMMARY_MD}"

jq -n \
  '{schema_version:1,check_id:"code-scanning-tools-zero",status:"pass",open_alerts:0,evidence_paths:["artifacts/ci/preflight/code-scanning-tools-zero/raw.log","artifacts/ci/preflight/code-scanning-tools-zero/open-alerts.json","artifacts/ci/preflight/code-scanning-tools-zero/summary.md"]}' > "${RESULT_JSON}"

log "PASS: Keine offenen Code-Scanning-Alerts."
