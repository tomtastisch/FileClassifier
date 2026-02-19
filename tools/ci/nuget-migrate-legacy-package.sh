#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
LC_ALL=C

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
SSOT_FILE="${REPO_ROOT}/tools/ci/policies/data/naming.json"
ARTIFACT_PLAN="${REPO_ROOT}/artifacts/legacy_migration_plan.txt"
NUGET_SOURCE="${NUGET_SOURCE:-https://api.nuget.org/v3/index.json}"
APPLY="${APPLY:-0}"
NUGET_API_KEY="${NUGET_API_KEY:-}"

mkdir -p "${REPO_ROOT}/artifacts"

usage() {
  cat <<'USAGE'
Usage:
  APPLY=1 NUGET_API_KEY=<key> bash tools/ci/nuget-migrate-legacy-package.sh

Behavior:
  - Reads canonical and deprecated package ids from tools/ci/policies/data/naming.json
  - Unlists all versions of each deprecated package via 'dotnet nuget delete' (nuget.org semantics)
  - Attempts deprecation via nuget CLI if supported, otherwise prints exact UI steps and exits non-zero when APPLY=1
  - Writes plan/evidence to artifacts/legacy_migration_plan.txt
USAGE
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

read_ssot() {
  python3 "${REPO_ROOT}/tools/ci/bin/nuget_migration_helpers.py" read-ssot --ssot "$SSOT_FILE"
}

mapfile -t ssot_lines < <(read_ssot)
CANONICAL_PACKAGE_ID="${ssot_lines[0]:-}"
DEPRECATED_IDS=("${ssot_lines[@]:1}")

if [[ -z "${CANONICAL_PACKAGE_ID}" ]]; then
  echo "FAIL: package_id missing in ${SSOT_FILE}" >&2
  exit 1
fi
if [[ "${#DEPRECATED_IDS[@]}" -eq 0 ]]; then
  echo "FAIL: deprecated_package_ids missing/empty in ${SSOT_FILE}" >&2
  exit 1
fi

for dep in "${DEPRECATED_IDS[@]}"; do
  if [[ "${dep}" == "${CANONICAL_PACKAGE_ID}" ]]; then
    echo "FAIL: canonical package '${CANONICAL_PACKAGE_ID}' must not be part of deprecated_package_ids" >&2
    exit 1
  fi
done

if [[ "${APPLY}" == "1" && -z "${NUGET_API_KEY}" ]]; then
  echo "FAIL: APPLY=1 requires NUGET_API_KEY" >&2
  exit 1
fi

{
  echo "legacy_migration_plan"
  echo "canonical_package_id=${CANONICAL_PACKAGE_ID}"
  echo "nuget_source=${NUGET_SOURCE}"
  echo "apply=${APPLY}"
} > "${ARTIFACT_PLAN}"

if [[ "${APPLY}" != "1" ]]; then
  echo "INFO: Dry-run mode (APPLY=${APPLY}). No server-side changes are executed." | tee -a "${ARTIFACT_PLAN}"
fi

supports_deprecate=0
if command -v nuget >/dev/null 2>&1; then
  if nuget help deprecate 2>/dev/null | rg -q "^NuGet"; then
    supports_deprecate=1
  fi
fi

for LEGACY_PACKAGE_ID in "${DEPRECATED_IDS[@]}"; do
  legacy_lc="$(printf '%s' "${LEGACY_PACKAGE_ID}" | tr '[:upper:]' '[:lower:]')"
  index_url="https://api.nuget.org/v3-flatcontainer/${legacy_lc}/index.json"

  versions_json="$(curl -fsSL "${index_url}")" || {
    echo "FAIL: unable to read versions for '${LEGACY_PACKAGE_ID}' from ${index_url}" | tee -a "${ARTIFACT_PLAN}" >&2
    exit 1
  }

  mapfile -t versions < <(python3 "${REPO_ROOT}/tools/ci/bin/nuget_migration_helpers.py" extract-versions --versions-json "${versions_json}")

  if [[ "${#versions[@]}" -eq 0 ]]; then
    echo "FAIL: no versions found for '${LEGACY_PACKAGE_ID}'" | tee -a "${ARTIFACT_PLAN}" >&2
    exit 1
  fi

  {
    echo ""
    echo "deprecated_package_id=${LEGACY_PACKAGE_ID}"
    echo "versions_count=${#versions[@]}"
  } >> "${ARTIFACT_PLAN}"

  for ver in "${versions[@]}"; do
    if [[ "${APPLY}" == "1" ]]; then
      echo "INFO: unlist ${LEGACY_PACKAGE_ID} ${ver}" | tee -a "${ARTIFACT_PLAN}"
      dotnet nuget delete "${LEGACY_PACKAGE_ID}" "${ver}" --api-key "${NUGET_API_KEY}" --source "${NUGET_SOURCE}" --non-interactive
    else
      echo "DRYRUN: dotnet nuget delete '${LEGACY_PACKAGE_ID}' '${ver}' --api-key '***' --source '${NUGET_SOURCE}' --non-interactive" | tee -a "${ARTIFACT_PLAN}"
    fi
  done

  if [[ "${supports_deprecate}" -eq 1 ]]; then
    for ver in "${versions[@]}"; do
      if [[ "${APPLY}" == "1" ]]; then
        echo "INFO: deprecate ${LEGACY_PACKAGE_ID} ${ver} -> ${CANONICAL_PACKAGE_ID}" | tee -a "${ARTIFACT_PLAN}"
        nuget deprecate "${LEGACY_PACKAGE_ID}" "${ver}" \
          -ApiKey "${NUGET_API_KEY}" \
          -Source "${NUGET_SOURCE}" \
          -AlternativePackage "${CANONICAL_PACKAGE_ID}" \
          -Message "Deprecated: use ${CANONICAL_PACKAGE_ID}." \
          -NonInteractive
      else
        echo "DRYRUN: nuget deprecate '${LEGACY_PACKAGE_ID}' '${ver}' -ApiKey '***' -Source '${NUGET_SOURCE}' -AlternativePackage '${CANONICAL_PACKAGE_ID}' -Message 'Deprecated: use ${CANONICAL_PACKAGE_ID}.' -NonInteractive" | tee -a "${ARTIFACT_PLAN}"
      fi
    done
  else
    {
      echo "WARN: local NuGet CLI does not support 'deprecate'."
      echo "ACTION REQUIRED (nuget.org UI):"
      echo "1) Open https://www.nuget.org/packages/${LEGACY_PACKAGE_ID}"
      echo "2) Manage Package -> Deprecation"
      echo "3) Select all legacy versions"
      echo "4) Set alternative package: ${CANONICAL_PACKAGE_ID}"
      echo "5) Save deprecation"
      echo "6) Verify deprecation banner and alternative package"
    } | tee -a "${ARTIFACT_PLAN}"

    if [[ "${APPLY}" == "1" ]]; then
      echo "FAIL: unlisting executed, but deprecation could not be automated with current CLI." | tee -a "${ARTIFACT_PLAN}" >&2
      exit 2
    fi
  fi
done

echo "OK: legacy migration plan completed." | tee -a "${ARTIFACT_PLAN}"
