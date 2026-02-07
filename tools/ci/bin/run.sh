#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd -- "${SCRIPT_DIR}/../../.." && pwd)"

# shellcheck source=tools/ci/lib/result.sh
source "${ROOT_DIR}/tools/ci/lib/result.sh"

CHECK_ID="${1:-}"
if [[ -z "$CHECK_ID" ]]; then
  echo "Usage: tools/ci/bin/run.sh <check_id>" >&2
  exit 2
fi

OUT_DIR="artifacts/ci/${CHECK_ID}"

if [[ "$CHECK_ID" == "artifact_contract" ]]; then
  cd "$ROOT_DIR"
  dotnet restore --locked-mode "${ROOT_DIR}/tools/ci/checks/PolicyRunner/PolicyRunner.csproj"
  dotnet build -c Release "${ROOT_DIR}/tools/ci/checks/PolicyRunner/PolicyRunner.csproj"
  exec dotnet "${ROOT_DIR}/tools/ci/checks/PolicyRunner/bin/Release/net10.0/PolicyRunner.dll" --check-id artifact_contract --repo-root "${ROOT_DIR}" --out-dir "${OUT_DIR}"
fi

ci_result_init "$CHECK_ID" "$OUT_DIR"

finalized=0
finalize_and_exit() {
  if [[ "$finalized" -eq 0 ]]; then
    ci_result_finalize
    finalized=1
  fi
}
trap finalize_and_exit EXIT
trap 'ci_result_append_summary "Check '\''${CHECK_ID}'\'' failed."' ERR

run_or_fail() {
  local rule_id="$1"
  local message="$2"
  shift 2
  if ! ci_run_capture "$message" "$@"; then
    ci_result_add_violation "$rule_id" "fail" "$message" "$CI_RAW_LOG"
    return 1
  fi
}

build_validators() {
  run_or_fail "CI-SETUP-001" "Restore validator projects (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/tools/ci/checks/ResultSchemaValidator/ResultSchemaValidator.csproj"
  run_or_fail "CI-SETUP-001" "Build ResultSchemaValidator" dotnet build -c Release "${ROOT_DIR}/tools/ci/checks/ResultSchemaValidator/ResultSchemaValidator.csproj"
  run_or_fail "CI-SETUP-001" "Restore PolicyRunner (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/tools/ci/checks/PolicyRunner/PolicyRunner.csproj"
  run_or_fail "CI-SETUP-001" "Build PolicyRunner" dotnet build -c Release "${ROOT_DIR}/tools/ci/checks/PolicyRunner/PolicyRunner.csproj"
  run_or_fail "CI-SETUP-001" "Restore CiGraphValidator (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/tools/ci/checks/CiGraphValidator/CiGraphValidator.csproj"
  run_or_fail "CI-SETUP-001" "Build CiGraphValidator" dotnet build -c Release "${ROOT_DIR}/tools/ci/checks/CiGraphValidator/CiGraphValidator.csproj"
  run_or_fail "CI-SETUP-001" "Restore QodanaContractValidator (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/tools/ci/checks/QodanaContractValidator/QodanaContractValidator.csproj"
  run_or_fail "CI-SETUP-001" "Build QodanaContractValidator" dotnet build -c Release "${ROOT_DIR}/tools/ci/checks/QodanaContractValidator/QodanaContractValidator.csproj"
}

run_preflight() {
  build_validators
  run_or_fail "CI-PREFLIGHT-001" "Label engine tests" node "${ROOT_DIR}/tools/versioning/test-compute-pr-labels.js"
  run_or_fail "CI-PREFLIGHT-001" "Docs check" python3 "${ROOT_DIR}/tools/check-docs.py"
  run_or_fail "CI-PREFLIGHT-001" "Versioning guard" bash "${ROOT_DIR}/tools/versioning/check-versioning.sh"
  run_or_fail "CI-PREFLIGHT-001" "Format check" dotnet format "${ROOT_DIR}/FileClassifier.sln" --verify-no-changes
  run_or_fail "CI-PREFLIGHT-001" "Policy shell safety" bash "${ROOT_DIR}/tools/ci/policies/policy_shell_safety.sh"
  run_or_fail "CI-GRAPH-001" "CI graph assertion" bash "${ROOT_DIR}/tools/ci/bin/assert_ci_graph.sh"

  ci_result_append_summary "Preflight checks completed."
}

run_build() {
  run_or_fail "CI-BUILD-001" "Restore solution (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/FileClassifier.sln" -v minimal
  run_or_fail "CI-BUILD-001" "Build solution" dotnet build "${ROOT_DIR}/FileClassifier.sln" --no-restore -warnaserror -v minimal
  ci_result_append_summary "Build completed."
}

run_security_nuget() {
  run_or_fail "CI-SECURITY-002" "Restore solution (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/FileClassifier.sln" -v minimal
  run_or_fail "CI-SECURITY-002" "NuGet vulnerability scan" dotnet list "${ROOT_DIR}/FileClassifier.sln" package --vulnerable --include-transitive

  if rg -n "\\b(High|Critical)\\b" "$CI_RAW_LOG" >/dev/null; then
    ci_result_add_violation "CI-SECURITY-001" "fail" "High/Critical NuGet vulnerabilities detected" "$CI_RAW_LOG"
    ci_result_append_summary "High/Critical NuGet vulnerabilities detected."
    return 1
  fi

  run_or_fail "CI-SECURITY-002" "NuGet deprecated packages" dotnet list "${ROOT_DIR}/FileClassifier.sln" package --deprecated
  ci_result_append_summary "NuGet security checks completed."
}

run_tests_bdd_coverage() {
  local tests_dir="${OUT_DIR}/tests"
  local coverage_dir="${OUT_DIR}/coverage"
  mkdir -p "$tests_dir" "$coverage_dir"

  run_or_fail "CI-TEST-001" "Restore solution (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/FileClassifier.sln" -v minimal
  run_or_fail "CI-TEST-001" "BDD tests + coverage" env TEST_BDD_OUTPUT_DIR="$tests_dir" bash "${ROOT_DIR}/tools/test-bdd-readable.sh" -- /p:CollectCoverage=true /p:Include="[FileTypeDetectionLib]*" /p:CoverletOutputFormat=cobertura /p:CoverletOutput="${coverage_dir}/coverage" /p:Threshold=85%2c69 /p:ThresholdType=line%2cbranch /p:ThresholdStat=total
  ci_result_append_summary "BDD coverage checks completed."
}

run_summary() {
  build_validators
  run_or_fail "CI-ARTIFACT-001" "Artifact contract policy" bash "${ROOT_DIR}/tools/ci/policies/policy_artifact_contract.sh" preflight build security-nuget tests-bdd-coverage
  ci_result_append_summary "Summary contract checks completed."
}

run_pr_labeling() {
  run_or_fail "CI-LABEL-001" "Fetch PR head" git fetch --no-tags --prune origin "${GITHUB_SHA}"

  local pr_number
  pr_number="$(jq -r '.pull_request.number // empty' "${GITHUB_EVENT_PATH}")"
  if [[ -z "$pr_number" ]]; then
    ci_result_add_violation "CI-LABEL-001" "fail" "pull_request number missing in event payload" "$GITHUB_EVENT_PATH"
    return 1
  fi

  local head_sha
  head_sha="$(jq -r '.pull_request.head.sha // empty' "${GITHUB_EVENT_PATH}")"

  run_or_fail "CI-LABEL-001" "Derive versioning decision" env BASE_REF=origin/main HEAD_REF="$head_sha" "${ROOT_DIR}/tools/versioning/check-versioning.sh"

  local files_json labels_json pr_title
  files_json="$(gh api "repos/${GITHUB_REPOSITORY}/pulls/${pr_number}/files" --paginate --jq '[.[].filename]')"
  labels_json="$(gh api "repos/${GITHUB_REPOSITORY}/issues/${pr_number}" --jq '[.labels[].name]')"
  pr_title="$(gh api "repos/${GITHUB_REPOSITORY}/pulls/${pr_number}" --jq '.title')"

  mkdir -p "${OUT_DIR}"
  FILES_JSON="$files_json" EXISTING_LABELS_JSON="$labels_json" PR_TITLE="$pr_title" VERSION_REQUIRED="none" VERSION_ACTUAL="none" VERSION_REASON="contract-run" VERSION_GUARD_EXIT="0" OUTPUT_PATH="${OUT_DIR}/decision.json" \
    ci_run_capture "Compute deterministic labels" node "${ROOT_DIR}/tools/versioning/compute-pr-labels.js"

  run_or_fail "CI-LABEL-001" "Validate label decision" node "${ROOT_DIR}/tools/versioning/validate-label-decision.js" "${ROOT_DIR}/tools/versioning/label-schema.json" "${OUT_DIR}/decision.json"
  ci_result_append_summary "PR labeling checks completed."
}

run_qodana_contract() {
  build_validators
  local sarif_path="${OUT_DIR}/qodana.sarif.json"
  if ! ci_run_capture "Qodana contract validator" dotnet "${ROOT_DIR}/tools/ci/checks/QodanaContractValidator/bin/Release/net10.0/QodanaContractValidator.dll" --sarif "$sarif_path"; then
    if rg -q "CI-QODANA-001" "$CI_RAW_LOG"; then
      ci_result_add_violation "CI-QODANA-001" "fail" "QODANA_TOKEN missing" "$CI_RAW_LOG"
    elif rg -q "CI-QODANA-002" "$CI_RAW_LOG"; then
      ci_result_add_violation "CI-QODANA-002" "fail" "Qodana SARIF missing" "$CI_RAW_LOG"
    elif rg -q "CI-QODANA-003" "$CI_RAW_LOG"; then
      ci_result_add_violation "CI-QODANA-003" "fail" "Qodana SARIF invalid" "$CI_RAW_LOG"
    else
      ci_result_add_violation "CI-QODANA-001" "fail" "Qodana contract validation failed" "$CI_RAW_LOG"
    fi
    return 1
  fi
  ci_result_append_summary "Qodana contract validation completed."
}

main() {
  cd "$ROOT_DIR"
  case "$CHECK_ID" in
    preflight) run_preflight ;;
    build) run_build ;;
    security-nuget) run_security_nuget ;;
    tests-bdd-coverage) run_tests_bdd_coverage ;;
    summary) run_summary ;;
    pr-labeling) run_pr_labeling ;;
    qodana) run_qodana_contract ;;
    *)
      ci_result_add_violation "CI-RUNNER-001" "fail" "unknown check_id '${CHECK_ID}'" "tools/ci/bin/run.sh"
      return 2
      ;;
  esac
}

main

if [[ "$(cat "$CI_STATUS_FILE")" == "fail" ]]; then
  ci_result_append_summary "Check '${CHECK_ID}' failed."
  exit 1
fi

ci_result_append_summary "Check '${CHECK_ID}' passed."
