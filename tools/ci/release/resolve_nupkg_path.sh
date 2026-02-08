#!/usr/bin/env bash
set -euo pipefail

out_file="${GITHUB_OUTPUT:?GITHUB_OUTPUT is required}"
search_dir="${1:-artifacts/nuget}"
nupkg_path="$(find "${search_dir}" -maxdepth 1 -type f -name '*.nupkg' ! -name '*.snupkg' | head -n1)"
[[ -n "${nupkg_path}" ]] || { echo "No .nupkg found in ${search_dir}" >&2; exit 1; }
echo "path=${nupkg_path}" >> "${out_file}"
