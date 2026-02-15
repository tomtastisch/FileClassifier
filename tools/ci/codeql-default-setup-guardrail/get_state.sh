#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
export LC_ALL=C

repo="${GITHUB_REPOSITORY:-${REPO:-}}"
if [[ -z "${repo}" ]]; then
  echo "ERROR: missing GITHUB_REPOSITORY/REPO env (expected owner/name)." >&2
  exit 2
fi
if [[ ! "${repo}" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]]; then
  echo "ERROR: invalid repo slug: '${repo}' (expected owner/name)." >&2
  exit 2
fi

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

gh_api() {
  local -r endpoint="$1"; shift
  if [[ -n "${CODEQL_TOKEN}" ]]; then
    GH_TOKEN="${CODEQL_TOKEN}" GH_REPO="${repo}" gh api "${endpoint}" "$@"
  else
    GH_REPO="${repo}" gh api "${endpoint}" "$@"
  fi
}

state=""
# Backoff schedule for max=4, base=1s: 1s, 2s, 4s.
if ! state="$(retry 4 1 gh_api "repos/{owner}/{repo}/code-scanning/default-setup" --jq .state)"; then
  echo "ERROR: failed to query CodeQL default-setup state via GitHub API." >&2
  exit 3
fi
if [[ -z "${state}" ]]; then
  echo "ERROR: invalid API response (missing state)." >&2
  exit 3
fi

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
