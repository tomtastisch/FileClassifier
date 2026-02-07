#!/usr/bin/env bash
set -euo pipefail

# shellcheck source=tools/ci/lib/result.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)/lib/result.sh"

if [[ $# -lt 1 ]]; then
  echo "Usage: policy_artifact_contract.sh <check_id> [<check_id> ...]" >&2
  exit 2
fi

has_failures=0

for check_id in "$@"; do
  check_dir="artifacts/ci/${check_id}"
  for req in raw.log summary.md result.json; do
    if [[ ! -f "${check_dir}/${req}" ]]; then
      ci_result_add_violation "CI-ARTIFACT-001" "fail" "missing required artifact ${check_dir}/${req}" "${check_dir}/${req}"
      has_failures=1
      continue
    fi
  done

  if [[ -f "${check_dir}/result.json" ]]; then
    if ! dotnet tools/ci/checks/ResultSchemaValidator/bin/Release/net10.0/ResultSchemaValidator.dll --schema tools/ci/schema/result.schema.json --result "${check_dir}/result.json" >> "$CI_RAW_LOG" 2>&1; then
      ci_result_add_violation "CI-SCHEMA-001" "fail" "result.json schema validation failed for ${check_id}" "${check_dir}/result.json"
      has_failures=1
    fi
  fi
done

if [[ "$has_failures" -eq 1 ]]; then
  exit 1
fi
