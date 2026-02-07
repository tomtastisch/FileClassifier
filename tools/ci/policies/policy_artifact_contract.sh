#!/usr/bin/env bash
set -euo pipefail

CI_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_ROOT="$(cd -- "${CI_DIR}/../.." && pwd)"

# shellcheck source=tools/ci/lib/result.sh
source "${CI_DIR}/lib/result.sh"

POLICY_OUT_DIR="${REPO_ROOT}/artifacts/ci/_policy_summary"
mkdir -p "${POLICY_OUT_DIR}"

policy_exit=0
if ! dotnet "${CI_DIR}/checks/PolicyRunner/bin/Release/net10.0/PolicyRunner.dll" --check-id summary --repo-root "${REPO_ROOT}" --out-dir "${POLICY_OUT_DIR}" >> "$CI_RAW_LOG" 2>&1; then
  policy_exit=1
fi

policy_result_json="${POLICY_OUT_DIR}/result.json"
policy_result_evidence="artifacts/ci/_policy_summary/result.json"
if [[ ! -f "${policy_result_json}" ]]; then
  ci_result_add_violation "CI-POLICY-001" "fail" "PolicyRunner did not produce result.json" "${policy_result_evidence}"
  ci_result_append_summary "Summary artifact contract policy failed (missing result.json)."
  exit 1
fi

findings=0
has_fail=0

while IFS= read -r violation; do
  rule_id="$(jq -r '.rule_id' <<< "$violation")"
  severity="$(jq -r '.severity' <<< "$violation")"
  message="$(jq -r '.message' <<< "$violation")"

  mapfile -t evidence_paths < <(jq -r '.evidence_paths[]' <<< "$violation")
  if [[ "${#evidence_paths[@]}" -eq 0 ]]; then
    evidence_paths=("tools/ci/policies/policy_artifact_contract.sh")
  fi

  ci_result_add_violation "$rule_id" "$severity" "$message" "${evidence_paths[@]}"
  findings=$((findings + 1))
  if [[ "$severity" == "fail" ]]; then
    has_fail=1
  fi
done < <(jq -c '.rule_violations[]' "${policy_result_json}")

if [[ "$findings" -eq 0 ]]; then
  ci_result_append_summary "Summary artifact contract policy passed."
else
  ci_result_append_summary "Summary artifact contract policy violations: $findings"
fi

if [[ "$policy_exit" -ne 0 || "$has_fail" -eq 1 ]]; then
  exit 1
fi
