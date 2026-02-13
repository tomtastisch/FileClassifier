#!/usr/bin/env bash
set -euo pipefail

nupkg_path="${1:?nupkg path required}"
source_url="${2:?github package source url required}"
api_key="${GITHUB_TOKEN:-}"

[[ -n "${api_key}" ]] || { echo "GITHUB_TOKEN is missing." >&2; exit 1; }

dotnet nuget push "${nupkg_path}" --api-key "${api_key}" --source "${source_url}" --skip-duplicate
