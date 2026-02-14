#!/usr/bin/env bash
set -euo pipefail

repo="${GITHUB_REPOSITORY:-${REPO:-}}"
if [[ -z "${repo}" ]]; then
  echo "ERROR: missing GITHUB_REPOSITORY/REPO env (expected owner/name)." >&2
  exit 2
fi

BASE_GH_TOKEN="${GH_TOKEN:-}"
CODEQL_TOKEN="${CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN:-}"

retry() {
  local -r max="${1}"; shift
  local -r base_sleep="${1}"; shift
  local attempt=1
  local sleep_s="${base_sleep}"
  while true; do
    if "$@"; then
      return 0
    fi
    if [[ "${attempt}" -ge "${max}" ]]; then
      return 1
    fi
    echo "WARN: command failed (attempt ${attempt}/${max}); retrying in ${sleep_s}s..." >&2
    sleep "${sleep_s}"
    attempt="$((attempt + 1))"
    sleep_s="$((sleep_s * 2))"
  done
}

state=""
if [[ -n "${CODEQL_TOKEN}" ]]; then
  export GH_TOKEN="${CODEQL_TOKEN}"
fi
if ! state="$(retry 4 1 gh api "repos/${repo}/code-scanning/default-setup" --jq .state)"; then
  export GH_TOKEN="${BASE_GH_TOKEN}"
  echo "ERROR: failed to query CodeQL default-setup state via GitHub API." >&2
  exit 3
fi
export GH_TOKEN="${BASE_GH_TOKEN}"

drift="false"
if [[ "${state}" != "not-configured" ]]; then
  drift="true"
fi

echo "INFO: default-setup state='${state}', drift='${drift}'."

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    echo "state=${state}"
    echo "drift=${drift}"
  } >> "${GITHUB_OUTPUT}"
fi
