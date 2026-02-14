#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
export LC_ALL=C

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT_DIR="${ROOT_DIR}/artifacts/ci/preflight/codeql-default-setup-guardrail"
RAW_LOG="${OUT_DIR}/raw.log"
SUMMARY_MD="${OUT_DIR}/summary.md"
RESULT_JSON="${OUT_DIR}/result.json"
DEFAULT_SETUP_JSON="${OUT_DIR}/default-setup.json"

mkdir -p "${OUT_DIR}"
: > "${RAW_LOG}"

BASE_GH_TOKEN="${GH_TOKEN:-}"
CODEQL_TOKEN="${CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN:-}"

log() {
  printf '%s\n' "$*" | tee -a "${RAW_LOG}" >/dev/null
}

fail() {
  local reason="$1"
  log "FAIL: ${reason}"
  {
    echo "# CodeQL Default Setup Guardrail"
    echo
    echo "- status: fail"
    echo "- reason: ${reason}"
  } > "${SUMMARY_MD}"
  jq -n --arg reason "${reason}" \
    '{schema_version:1,check_id:"codeql-default-setup-guardrail",status:"fail",reason:$reason,evidence_paths:["artifacts/ci/preflight/codeql-default-setup-guardrail/raw.log","artifacts/ci/preflight/codeql-default-setup-guardrail/summary.md"]}' \
    > "${RESULT_JSON}"
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
  if [[ -n "${CODEQL_TOKEN}" ]]; then
    export GH_TOKEN="${CODEQL_TOKEN}"
  fi
  if gh api "repos/${REPO}/code-scanning/default-setup" > "${DEFAULT_SETUP_JSON}" 2>> "${RAW_LOG}"; then
    export GH_TOKEN="${BASE_GH_TOKEN}"
    break
  fi
  export GH_TOKEN="${BASE_GH_TOKEN}"
  if (( attempt >= max_attempts )); then
    if grep -qF "Resource not accessible by integration (HTTP 403)" "${RAW_LOG}"; then
      fail "GitHub API 403 fuer CodeQL Default Setup. GITHUB_TOKEN reicht hier nicht aus; setze Secret CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN (Fine-Grained PAT, Repo: Administration Read, Security Events Read)."
    fi
    fail "GitHub API fuer CodeQL Default Setup fehlgeschlagen"
  fi
  log "WARN: API-Fehler, retry ${attempt}/${max_attempts}"
  sleep "${delay}"
  attempt=$((attempt + 1))
  delay=$((delay * 2))
done

state="$(jq -r '.state // empty' "${DEFAULT_SETUP_JSON}")"
if [[ -z "${state}" ]]; then
  fail "Ungueltige API-Antwort: state fehlt"
fi

if [[ "${state}" != "not-configured" ]]; then
  fail "CodeQL Default Setup ist aktiv (state=${state}). Advanced-Setup erfordert state=not-configured."
fi

{
  echo "# CodeQL Default Setup Guardrail"
  echo
  echo "- status: pass"
  echo "- repo: ${REPO}"
  echo "- state: ${state}"
} > "${SUMMARY_MD}"

jq -n \
  '{schema_version:1,check_id:"codeql-default-setup-guardrail",status:"pass",state:"not-configured",evidence_paths:["artifacts/ci/preflight/codeql-default-setup-guardrail/raw.log","artifacts/ci/preflight/codeql-default-setup-guardrail/default-setup.json","artifacts/ci/preflight/codeql-default-setup-guardrail/summary.md"]}' \
  > "${RESULT_JSON}"

log "PASS: CodeQL Default Setup ist not-configured."
