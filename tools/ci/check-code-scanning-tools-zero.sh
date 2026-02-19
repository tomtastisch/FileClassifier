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

# Gate strategy:
# - Default: enforce 0 open alerts on main (prevents unrelated merges while main is red).
# - Exception: Qodana-cleanup PRs validate against the PR ref, so the cleanup PR itself can merge.
#   Detection is fail-safe and deterministic:
#   1) explicit label "area:qodana", OR
#   2) changed files include qodana paths (independent of label-job timing).
EVENT_NAME="${GITHUB_EVENT_NAME:-}"
QUERY_REF=""
if [[ "${EVENT_NAME}" == "pull_request" && -n "${GITHUB_EVENT_PATH:-}" && -f "${GITHUB_EVENT_PATH:-}" ]]; then
  pr_number="$(jq -r '.pull_request.number // empty' "${GITHUB_EVENT_PATH}")"
  has_qodana_label="false"
  if jq -r '.pull_request.labels[].name // empty' "${GITHUB_EVENT_PATH}" | grep -Fxq -- "area:qodana"; then
    has_qodana_label="true"
  fi

  has_qodana_changes="false"
  if [[ -n "${pr_number}" ]]; then
    files_json="${OUT_DIR}/pr-files.json"
    if gh api "repos/${REPO}/pulls/${pr_number}/files?per_page=100" --paginate > "${files_json}" 2>> "${RAW_LOG}"; then
      if jq -r '.[].filename' "${files_json}" | grep -Eiq '^(\.qodana/|qodana\.ya?ml$|\.github/workflows/qodana\.yml$)'; then
        has_qodana_changes="true"
      fi
    else
      log "WARN: PR files konnten nicht geladen werden; fallback auf Label-basierte Erkennung."
    fi
  fi

  if [[ "${has_qodana_label}" == "true" || "${has_qodana_changes}" == "true" ]]; then
    QUERY_REF="${GITHUB_REF:-}"
    if [[ -z "${QUERY_REF}" ]]; then
      if [[ -n "${pr_number}" ]]; then
        QUERY_REF="refs/pull/${pr_number}/merge"
      fi
    fi
    log "INFO: Qodana-PR erkannt (label=${has_qodana_label}, files=${has_qodana_changes}) -> pruefe Code-Scanning Alerts fuer ref=${QUERY_REF:-<unset>}"
  else
    QUERY_REF="refs/heads/main"
    log "INFO: PR ohne Label area:qodana -> pruefe Code-Scanning Alerts fuer ref=${QUERY_REF}"
  fi
else
  QUERY_REF="${GITHUB_REF:-refs/heads/main}"
  log "INFO: Event=${EVENT_NAME:-<unset>} -> pruefe Code-Scanning Alerts fuer ref=${QUERY_REF}"
fi

# Alert state can lag until the qodana workflow for the same SHA uploads SARIF.
# Wait fail-closed in both cases:
# - main push validation (ref=main),
# - PR Qodana-cleanup validation (ref=refs/pull/<n>/merge).
if [[ -n "${GITHUB_SHA:-}" ]]; then
  should_wait_for_qodana="false"
  if [[ "${EVENT_NAME}" != "pull_request" && "${QUERY_REF}" == "refs/heads/main" ]]; then
    should_wait_for_qodana="true"
  fi
  if [[ "${QUERY_REF}" != "refs/heads/main" ]]; then
    should_wait_for_qodana="true"
  fi

  if [[ "${should_wait_for_qodana}" == "true" ]]; then
  wait_attempt=1
  # Qodana can be queued behind runner load; allow a generous fail-closed window.
  wait_max_attempts=120
  wait_delay=10
  run_event_filter="${EVENT_NAME:-push}"
  while true; do
    qodana_runs_json="${OUT_DIR}/qodana-runs.json"
    if ! gh api "repos/${REPO}/actions/runs?head_sha=${GITHUB_SHA}&event=${run_event_filter}&per_page=100" > "${qodana_runs_json}" 2>> "${RAW_LOG}"; then
      if (( wait_attempt >= wait_max_attempts )); then
        fail "Qodana-Runstatus fuer aktuellen SHA konnte nicht geladen werden"
      fi
      log "WARN: Qodana-Runstatus API-Fehler, retry ${wait_attempt}/${wait_max_attempts}"
      sleep "${wait_delay}"
      wait_attempt=$((wait_attempt + 1))
      continue
    fi

    qodana_status="$(jq -r '.workflow_runs[] | select(.name=="qodana") | .status' "${qodana_runs_json}" | head -n1)"
    qodana_conclusion="$(jq -r '.workflow_runs[] | select(.name=="qodana") | .conclusion' "${qodana_runs_json}" | head -n1)"
    qodana_url="$(jq -r '.workflow_runs[] | select(.name=="qodana") | .html_url' "${qodana_runs_json}" | head -n1)"

    if [[ -z "${qodana_status}" ]]; then
      if (( wait_attempt >= wait_max_attempts )); then
        fail "Kein qodana-Run fuer SHA=${GITHUB_SHA} gefunden"
      fi
      log "INFO: qodana-Run fuer SHA=${GITHUB_SHA} noch nicht sichtbar (retry ${wait_attempt}/${wait_max_attempts})"
      sleep "${wait_delay}"
      wait_attempt=$((wait_attempt + 1))
      continue
    fi

    if [[ "${qodana_status}" != "completed" ]]; then
      if (( wait_attempt >= wait_max_attempts )); then
        fail "qodana-Run fuer SHA=${GITHUB_SHA} ist nicht abgeschlossen (status=${qodana_status})"
      fi
      log "INFO: warte auf qodana-Runabschluss (status=${qodana_status}, retry ${wait_attempt}/${wait_max_attempts})"
      sleep "${wait_delay}"
      wait_attempt=$((wait_attempt + 1))
      continue
    fi

    if [[ "${qodana_conclusion}" != "success" ]]; then
      fail "qodana-Run fuer SHA=${GITHUB_SHA} ist fehlgeschlagen (conclusion=${qodana_conclusion:-unknown}, url=${qodana_url:-n/a})"
    fi

    log "INFO: qodana-Run fuer SHA=${GITHUB_SHA} erfolgreich abgeschlossen (${qodana_url:-n/a})"
    break
  done
  fi
fi

attempt=1
delay=2
max_attempts=3
while true; do
  api_path="repos/${REPO}/code-scanning/alerts?state=open&per_page=100"
  if [[ -n "${QUERY_REF}" ]]; then
    api_path="${api_path}&ref=${QUERY_REF}"
  fi
  if gh api "${api_path}" --paginate > "${ALERTS_JSON}" 2>> "${RAW_LOG}"; then
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

count="$(jq 'length' "${ALERTS_JSON}")"
if [[ "${count}" -gt 0 ]]; then
  jq '[.[0:50][] | {number,tool:.tool.name,rule:(.rule.id // .rule.name),security_severity:(.rule.security_severity_level // null),rule_severity:(.rule.severity // null),html_url}]' "${ALERTS_JSON}" > "${OUT_DIR}/open-alerts-summary.json"
  log "Open code-scanning alerts detected: ${count} (showing first 50)"
  jq -r '.[] | "- #\(.number) [\(.tool)] \(.rule) [security_sev=\(.security_severity) rule_sev=\(.rule_severity)] -> \(.html_url)"' "${OUT_DIR}/open-alerts-summary.json" | tee -a "${RAW_LOG}" >/dev/null
  fail "Offene Code-Scanning-Alerts vorhanden (${count})"
fi

{
  echo "# Code Scanning Tools Zero"
  echo
  echo "- status: pass"
  echo "- open_alerts: 0"
  echo "- ref: ${QUERY_REF}"
  echo "- repo: ${REPO}"
} > "${SUMMARY_MD}"

jq -n \
  '{schema_version:1,check_id:"code-scanning-tools-zero",status:"pass",open_alerts:0,evidence_paths:["artifacts/ci/preflight/code-scanning-tools-zero/raw.log","artifacts/ci/preflight/code-scanning-tools-zero/open-alerts.json","artifacts/ci/preflight/code-scanning-tools-zero/summary.md"]}' > "${RESULT_JSON}"

log "PASS: Keine offenen Code-Scanning-Alerts."
