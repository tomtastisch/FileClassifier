#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
export LC_ALL=C

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT_DIR="${ROOT_DIR}/artifacts/ci/preflight/pr-governance"
RAW_LOG="${OUT_DIR}/raw.log"
SUMMARY_MD="${OUT_DIR}/summary.md"

mkdir -p "${OUT_DIR}"
: > "${RAW_LOG}"

log() {
  printf '%s\n' "$*" | tee -a "${RAW_LOG}" >/dev/null
}

fail() {
  log "FAIL: $*"
  printf 'FAIL: %s\n' "$*" >&2
  {
    echo "# PR Governance"
    echo
    echo "- status: fail"
    echo "- reason: $*"
  } > "${SUMMARY_MD}"
  exit 1
}

pass() {
  {
    echo "# PR Governance"
    echo
    echo "- status: pass"
    echo "- branch: ${BRANCH_NAME}"
    echo "- title: ${PR_TITLE}"
    echo "- checklists: ${CHECKLIST_COUNT}"
  } > "${SUMMARY_MD}"
}

if ! command -v jq >/dev/null 2>&1; then
  fail "jq fehlt"
fi

if [[ -z "${GITHUB_EVENT_PATH:-}" || ! -f "${GITHUB_EVENT_PATH:-}" ]]; then
  if [[ "${CI:-}" == "true" ]]; then
    fail "GITHUB_EVENT_PATH fehlt"
  fi
  log "INFO: Lokaler Lauf ohne GITHUB_EVENT_PATH, Check wird als pass behandelt."
  BRANCH_NAME="local"
  PR_TITLE="local"
  CHECKLIST_COUNT=0
  pass
  exit 0
fi

EVENT_NAME="${GITHUB_EVENT_NAME:-}"
if [[ "${EVENT_NAME}" != "pull_request" ]]; then
  log "INFO: Kein pull_request Event, Check wird als pass behandelt."
  BRANCH_NAME="n/a"
  PR_TITLE="n/a"
  CHECKLIST_COUNT=0
  pass
  exit 0
fi

BRANCH_NAME="$(jq -r '.pull_request.head.ref // empty' "${GITHUB_EVENT_PATH}")"
PR_TITLE="$(jq -r '.pull_request.title // empty' "${GITHUB_EVENT_PATH}")"
PR_BODY="$(jq -r '.pull_request.body // ""' "${GITHUB_EVENT_PATH}")"

if [[ -z "${BRANCH_NAME}" ]]; then
  fail "Branch-Name aus Event nicht lesbar"
fi
if [[ -z "${PR_TITLE}" ]]; then
  fail "PR-Titel fehlt"
fi

BRANCH_REGEX='^codex/(fix|release|feat|refactor|docs|test|ci|chore|security)(/[a-z0-9][a-z0-9-]{2,}|-[a-z0-9][a-z0-9-]{2,})$'
TITLE_REGEX='^(fix|release|feat|refactor|docs|test|ci|chore|security)(\([a-z0-9-]+\))?: .+'

if [[ ! "${BRANCH_NAME}" =~ ${BRANCH_REGEX} ]]; then
  fail "Branch-Format ungueltig: ${BRANCH_NAME}"
fi
if [[ ! "${PR_TITLE}" =~ ${TITLE_REGEX} ]]; then
  fail "PR-Titel-Format ungueltig: ${PR_TITLE}"
fi

required_sections=(
  "## Ziel & Scope"
  "## Umgesetzte Aufgaben (abhaken)"
  "## Nachbesserungen aus Review (iterativ)"
  "## Security- und Merge-Gates"
  "## Evidence (auditierbar)"
  "## DoD (mindestens 2 pro Punkt)"
)

for section in "${required_sections[@]}"; do
  if ! grep -Fq -- "${section}" <<< "${PR_BODY}"; then
    fail "Pflichtsektion fehlt: ${section}"
  fi
done

CHECKLIST_COUNT="$(grep -E -c '^- \[[ xX]\]' <<< "${PR_BODY}" || true)"
if [[ "${CHECKLIST_COUNT}" -lt 8 ]]; then
  fail "Zu wenige Checklistenpunkte (${CHECKLIST_COUNT} < 8)"
fi

if ! grep -Fq -- "security/code-scanning/tools" <<< "${PR_BODY}" || ! grep -Fq -- "0 offene Alerts" <<< "${PR_BODY}"; then
  fail "Pflichtaussage fuer Code-Scanning-0-Alert fehlt"
fi

pass
log "PASS: PR Governance erfuellt"
