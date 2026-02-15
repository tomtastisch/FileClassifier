#!/usr/bin/env bash
set -euo pipefail

REPO="${REPO:?REPO required (owner/repo)}"
OWNER="${OWNER:-${REPO%%/*}}"
PACKAGE_ID="${PACKAGE_ID:-Tomtastisch.FileClassifier}"
NUGET_PACKAGE_ID="${NUGET_PACKAGE_ID:-tomtastisch.fileclassifier}"
OUT_DIR="${OUT_DIR:-artifacts/retention}"
DRY_RUN="${DRY_RUN:-0}"

GH_TOKEN="${GH_TOKEN:-${GITHUB_TOKEN:-}}"
NUGET_API_KEY="${NUGET_API_KEY:-}"

mkdir -p "${OUT_DIR}"
DECISION_JSON="${OUT_DIR}/decision.json"
SUMMARY_TSV="${OUT_DIR}/summary.tsv"
ACTIONS_LOG="${OUT_DIR}/actions.log"

if [[ -z "${GH_TOKEN}" ]]; then
  echo "GH_TOKEN/GITHUB_TOKEN missing" >&2
  exit 1
fi
NUGET_ENABLED="1"
if [[ -z "${NUGET_API_KEY}" ]]; then
  # Best-effort for scheduled retention: still keep GH Releases + GH Packages retention working,
  # but skip NuGet unlist when no API key is available.
  echo "WARN: NUGET_API_KEY missing; skipping NuGet retention actions" >&2
  NUGET_ENABLED="0"
fi

mapfile -t TAGS < <(gh api "/repos/${REPO}/tags" --paginate --jq '.[].name' | sort -u)

mapfile -t STABLE_TAGS < <(printf '%s\n' "${TAGS[@]}" | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | sort -V -r || true)
mapfile -t RC_TAGS < <(printf '%s\n' "${TAGS[@]}" | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+-rc\.[0-9]+$' | sort -V -r || true)

LATEST_STABLE="${STABLE_TAGS[0]:-}"
PREV_STABLE="${STABLE_TAGS[1]:-}"
BASELINE=""
if [[ -n "${LATEST_STABLE}" ]]; then
  base_no_v="${LATEST_STABLE#v}"
  IFS='.' read -r major minor patch <<<"${base_no_v}"
  candidate="v${major}.${minor}.0"
  if printf '%s\n' "${STABLE_TAGS[@]}" | grep -xF "${candidate}" >/dev/null 2>&1; then
    BASELINE="${candidate}"
  fi
fi
LATEST_RC="${RC_TAGS[0]:-}"

declare -A KEEP_TAGS=()
[[ -n "${LATEST_STABLE}" ]] && KEEP_TAGS["${LATEST_STABLE}"]=1
[[ -n "${PREV_STABLE}" ]] && KEEP_TAGS["${PREV_STABLE}"]=1
[[ -n "${BASELINE}" ]] && KEEP_TAGS["${BASELINE}"]=1
[[ -n "${LATEST_RC}" ]] && KEEP_TAGS["${LATEST_RC}"]=1

# GH releases actions
mapfile -t RELEASE_ROWS < <(gh api "/repos/${REPO}/releases" --paginate | jq -r '.[] | [.id, .tag_name] | @tsv')

# NuGet versions
mapfile -t NUGET_VERSIONS < <(curl -fsSL "https://api.nuget.org/v3-flatcontainer/${NUGET_PACKAGE_ID}/index.json" | jq -r '.versions[]' || true)

# GH packages versions (user endpoint by default; fallback org endpoint)
PACKAGE_LIST_ENDPOINT="/users/${OWNER}/packages/nuget/${PACKAGE_ID}/versions"
if ! gh api "${PACKAGE_LIST_ENDPOINT}" >/dev/null 2>&1; then
  PACKAGE_LIST_ENDPOINT="/orgs/${OWNER}/packages/nuget/${PACKAGE_ID}/versions"
fi
mapfile -t PACKAGE_ROWS < <(gh api "${PACKAGE_LIST_ENDPOINT}" --paginate | jq -r '.[] | [.id, .name] | @tsv' || true)

{
  echo '{'
  echo '  "schema_version": 1,'
  echo "  \"repo\": \"${REPO}\"," 
  echo "  \"timestamp_utc\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"," 
  echo '  "keep": {'
  echo "    \"latest_stable\": \"${LATEST_STABLE}\"," 
  echo "    \"previous_stable\": \"${PREV_STABLE}\"," 
  echo "    \"baseline\": \"${BASELINE}\"," 
  echo "    \"latest_rc\": \"${LATEST_RC}\""
  echo '  }'
  echo '}'
} > "${DECISION_JSON}"

echo -e "status\ttarget\taction\titem" > "${SUMMARY_TSV}"
: > "${ACTIONS_LOG}"

for row in "${RELEASE_ROWS[@]}"; do
  id="${row%%$'\t'*}"
  tag="${row#*$'\t'}"
  if [[ -n "${KEEP_TAGS[$tag]:-}" ]]; then
    echo -e "keep\tgh_release\tkeep\t${tag}" >> "${SUMMARY_TSV}"
    continue
  fi
  if [[ "${DRY_RUN}" == "1" ]]; then
    echo "DRY_RUN gh release delete ${tag}" >> "${ACTIONS_LOG}"
    echo -e "plan\tgh_release\tdelete\t${tag}" >> "${SUMMARY_TSV}"
  else
    gh release delete "${tag}" --repo "${REPO}" --yes
    echo "EXEC gh release delete ${tag}" >> "${ACTIONS_LOG}"
    echo -e "done\tgh_release\tdelete\t${tag}" >> "${SUMMARY_TSV}"
  fi
done

for version in "${NUGET_VERSIONS[@]}"; do
  tag="v${version}"
  if [[ -n "${KEEP_TAGS[$tag]:-}" ]]; then
    echo -e "keep\tnuget\tkeep\t${version}" >> "${SUMMARY_TSV}"
    continue
  fi
  if [[ "${NUGET_ENABLED}" != "1" ]]; then
    echo "SKIP NuGet unlist ${PACKAGE_ID} ${version} (reason=NUGET_API_KEY missing)" >> "${ACTIONS_LOG}"
    echo -e "skip\tnuget\tunlist\t${version} (reason=missing_api_key)" >> "${SUMMARY_TSV}"
    continue
  fi
  if [[ "${DRY_RUN}" == "1" ]]; then
    echo "DRY_RUN dotnet nuget delete ${PACKAGE_ID} ${version}" >> "${ACTIONS_LOG}"
    echo -e "plan\tnuget\tunlist\t${version}" >> "${SUMMARY_TSV}"
  else
    delete_log="${OUT_DIR}/nuget-delete-${version}.log"
    set +e
    dotnet nuget delete "${PACKAGE_ID}" "${version}" --api-key "${NUGET_API_KEY}" --source "https://api.nuget.org/v3/index.json" --non-interactive >"${delete_log}" 2>&1
    rc=$?
    set -e
    if [[ "${rc}" -eq 0 ]]; then
      echo "EXEC dotnet nuget delete ${PACKAGE_ID} ${version}" >> "${ACTIONS_LOG}"
      echo -e "done\tnuget\tunlist\t${version}" >> "${SUMMARY_TSV}"
      continue
    fi
    if grep -Eqi '(forbidden|unauthorized| 403 | 401 |status code.*403|status code.*401|does not have permission|api key is invalid|has expired)' "${delete_log}"; then
      echo "SKIP dotnet nuget delete ${PACKAGE_ID} ${version} (reason=403/401)" >> "${ACTIONS_LOG}"
      echo -e "skip\tnuget\tunlist\t${version} (reason=forbidden)" >> "${SUMMARY_TSV}"
      continue
    fi
    echo "FAIL dotnet nuget delete ${PACKAGE_ID} ${version} (rc=${rc})" >> "${ACTIONS_LOG}"
    echo -e "fail\tnuget\tunlist\t${version} (rc=${rc})" >> "${SUMMARY_TSV}"
    exit "${rc}"
  fi
done

for row in "${PACKAGE_ROWS[@]}"; do
  id="${row%%$'\t'*}"
  version="${row#*$'\t'}"
  tag="v${version}"
  if [[ -n "${KEEP_TAGS[$tag]:-}" ]]; then
    echo -e "keep\tgh_packages\tkeep\t${version}" >> "${SUMMARY_TSV}"
    continue
  fi
  if [[ "${DRY_RUN}" == "1" ]]; then
    echo "DRY_RUN gh api -X DELETE ${PACKAGE_LIST_ENDPOINT}/${id}" >> "${ACTIONS_LOG}"
    echo -e "plan\tgh_packages\tdelete\t${version}" >> "${SUMMARY_TSV}"
  else
    gh api -X DELETE "${PACKAGE_LIST_ENDPOINT}/${id}"
    echo "EXEC gh api -X DELETE ${PACKAGE_LIST_ENDPOINT}/${id}" >> "${ACTIONS_LOG}"
    echo -e "done\tgh_packages\tdelete\t${version}" >> "${SUMMARY_TSV}"
  fi
done

echo "retention: completed (dry_run=${DRY_RUN})"
