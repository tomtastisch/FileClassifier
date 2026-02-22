#!/usr/bin/env bash
set -euo pipefail

project_path="${1:?project path required}"

tmp_file="$(mktemp)"
trap 'rm -f "${tmp_file}"' EXIT

dotnet test "${project_path}" -c Release --no-build --filter "Category=Fuzz" -v minimal | tee "${tmp_file}"

if rg -q "Total tests:[[:space:]]*0|No test matches|Kein Test entspricht" "${tmp_file}"; then
  echo "Fuzz tests release gate failed: no tests were executed for filter Category=Fuzz." >&2
  exit 1
fi
