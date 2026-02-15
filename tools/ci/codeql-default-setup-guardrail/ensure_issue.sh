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
if ! state="$(retry 4 1 gh_api "repos/{owner}/{repo}/code-scanning/default-setup" --jq .state)"; then
  echo "ERROR: failed to query CodeQL default-setup state via GitHub API." >&2
  exit 3
fi
if [[ -z "${state}" ]]; then
  echo "ERROR: invalid API response (missing state)." >&2
  exit 3
fi

if [[ "${state}" == "not-configured" ]]; then
  echo "INFO: state is not-configured; no drift issue required."
  exit 0
fi

title="SECURITY: CodeQL Default Setup ist aktiviert (Guardrail)"
marker="<!-- codeql-default-setup-guardrail -->"

body=$'Der CI-Guardrail hat festgestellt, dass **GitHub CodeQL Default Setup** aktiv ist.\n\n'
body+=$'Impact:\n- Advanced CodeQL Workflow (`.github/workflows/codeql.yml`) kann dadurch keine SARIF-Ergebnisse wie erwartet verarbeiten.\n\n'
body+=$'Fix:\n- In GitHub UI: Settings -> Code security and analysis -> CodeQL -> Default setup deaktivieren\n- Oder per API: `PATCH /repos/{owner}/{repo}/code-scanning/default-setup` mit `state=not-configured`\n\n'
body+=$'Evidence:\n- Siehe Workflow-Logs und `artifacts/ci/preflight/codeql-default-setup-guardrail/`.\n\n'
if [[ ! "${state}" =~ ^[a-z-]+$ ]]; then
  state="unknown"
fi
body+="Observed state: \`${state}\`"$'\n\n'
body+="${marker}"$'\n'

existing="$(gh_api "repos/{owner}/{repo}/issues?state=open&per_page=100" --paginate --slurp | jq -r --arg t "${title}" '[.[] | .[] | select(.pull_request? | not) | select(.title == $t) | .number][0] // empty')"
if [[ -n "${existing}" ]]; then
  echo "INFO: drift issue already open (#${existing}); nothing to do."
  exit 0
fi

issue_number=""
if ! issue_number="$(gh_api "repos/{owner}/{repo}/issues" -X POST -f title="${title}" -f body="${body}" --jq .number)"; then
  # Benign race: another run may have created the issue after our initial check.
  existing_after_create="$(gh_api "repos/{owner}/{repo}/issues?state=open&per_page=100" --paginate --slurp | jq -r --arg t "${title}" '[.[] | .[] | select(.pull_request? | not) | select(.title == $t) | .number][0] // empty')"
  if [[ -n "${existing_after_create}" ]]; then
    issue_number="${existing_after_create}"
    echo "INFO: drift issue was created concurrently (#${issue_number}); proceeding."
  else
    echo "ERROR: failed to create drift issue in ${repo}." >&2
    exit 4
  fi
fi

echo "INFO: created drift issue #${issue_number}."

# Best-effort labels: only add labels that already exist, otherwise the API call would hard-fail.
desired_labels=("security" "area:pipeline")
labels_available="$(gh label list -R "${repo}" -L 200 --json name 2>/dev/null || echo '[]')"
for label in "${desired_labels[@]}"; do
  if jq -er --arg label "${label}" '.[] | select(.name == $label) | true' <<<"${labels_available}" >/dev/null 2>&1; then
    if gh issue edit "${issue_number}" -R "${repo}" --add-label "${label}" >/dev/null 2>&1; then
      echo "INFO: added label '${label}'."
    else
      echo "WARN: failed to add label '${label}' to issue #${issue_number}." >&2
    fi
  else
    echo "INFO: label '${label}' not present in repo; skipping."
  fi
done
