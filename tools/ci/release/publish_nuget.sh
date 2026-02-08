#!/usr/bin/env bash
set -euo pipefail

nupkg_path="${1:?nupkg path required}"
api_key="${NUGET_API_KEY:-}"
[[ -n "${api_key}" && "${api_key}" != "__REPLACE_WITH_NUGET_API_KEY__" ]] || { echo "NUGET_API_KEY is missing or placeholder." >&2; exit 1; }

dotnet nuget push "${nupkg_path}" --api-key "${api_key}" --source "https://api.nuget.org/v3/index.json" --skip-duplicate
