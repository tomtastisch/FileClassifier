#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
CHECK_ID="code-analysis-evidence"
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

validate_json_doc() {
  local file_path="$1"
  local jq_expr="$2"
  if [[ ! -f "${file_path}" ]]; then
    add_violation "CI-CODE-ANALYSIS-001" "fail" "Missing JSON artifact: ${file_path}" "${OUT_DIR_REL}/raw.log"
    return 1
  fi
  if ! jq -e "${jq_expr}" "${file_path}" >> "${RAW_LOG}" 2>&1; then
    add_violation "CI-CODE-ANALYSIS-002" "fail" "Invalid JSON structure in ${file_path}" "${file_path#${ROOT_DIR}/}"
    return 1
  fi
  add_pass
  return 0
}

if ! run_cmd_capture "Generate code analysis JSON artifacts" bash "${ROOT_DIR}/tools/audit/generate-code-analysis-json.sh"; then
  add_violation "CI-CODE-ANALYSIS-000" "fail" "Generator script failed" "tools/audit/generate-code-analysis-json.sh"
fi

validate_json_doc "${ROOT_DIR}/artifacts/audit/code_inventory.json" '.generated_at and (.files | type=="array")' || true
validate_json_doc "${ROOT_DIR}/artifacts/audit/callgraph_inventory.json" '.generated_at and (.method_declarations | type=="array") and (.symbol_reference_counts | type=="array") and (.edges | type=="array")' || true
validate_json_doc "${ROOT_DIR}/artifacts/audit/dead_code_candidates.json" '.generated_at and (.candidates | type=="array")' || true
validate_json_doc "${ROOT_DIR}/artifacts/audit/redundancy_candidates.json" '.generated_at and (.candidates | type=="array")' || true
validate_json_doc "${ROOT_DIR}/artifacts/audit/hardening_candidates.json" '.generated_at and (.candidates | type=="array")' || true

if [[ "${FAIL_COUNT}" -eq 0 ]]; then
  STATUS="pass"
else
  STATUS="fail"
fi

FINISHED_AT="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
END_EPOCH="$(date -u +%s)"
DURATION_MS="$(( (END_EPOCH - START_EPOCH) * 1000 ))"

{
  echo "# Code Analysis Evidence"
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
    "check_id": "code-analysis-evidence",
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
