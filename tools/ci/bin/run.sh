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

gh_retry() {
  local max_attempts="${GH_RETRY_MAX_ATTEMPTS:-4}"
  local delay_secs="${GH_RETRY_INITIAL_DELAY_SECS:-2}"
  local attempt=1

  while true; do
    if "$@"; then
      return 0
    fi
    if (( attempt >= max_attempts )); then
      return 1
    fi
    sleep "$delay_secs"
    attempt=$((attempt + 1))
    delay_secs=$((delay_secs * 2))
  done
}

log_contains_code() {
  local code="$1"
  if command -v rg >/dev/null 2>&1; then
    rg -q --fixed-strings "$code" "$CI_RAW_LOG"
  else
    grep -Fq -- "$code" "$CI_RAW_LOG"
  fi
}

log_contains_high_or_critical() {
  if command -v rg >/dev/null 2>&1; then
    rg -n "\\b(High|Critical)\\b" "$CI_RAW_LOG" >/dev/null
  else
    grep -En "(^|[^[:alnum:]_])(High|Critical)([^[:alnum:]_]|$)" "$CI_RAW_LOG" >/dev/null
  fi
}

find_pack_nupkg() {
  local pack_dir="$1"
  python3 - "${pack_dir}" <<'PY'
import glob
import os
import sys

pack_dir = sys.argv[1]
candidates = [
    path for path in glob.glob(os.path.join(pack_dir, "*.nupkg"))
    if not path.endswith(".snupkg")
]
if not candidates:
    sys.exit(1)

# Deterministic: prefer the most recently written package artifact.
candidates.sort(key=lambda path: os.path.getmtime(path), reverse=True)
print(candidates[0])
PY
}

read_nupkg_metadata() {
  local nupkg_path="$1"
  local field="$2"
  local nuspec_xml

  if [[ ! -f "${nupkg_path}" ]]; then
    return 1
  fi

  if ! nuspec_xml="$(unzip -p "${nupkg_path}" '*.nuspec' 2>/dev/null)"; then
    return 1
  fi

  printf '%s\n' "${nuspec_xml}" | tr -d '\r' | sed -n "s/.*<${field}>\\([^<]*\\)<\\/${field}>.*/\\1/p" | head -n1
}

read_naming_ssot_field() {
  local field="$1"
  local ssot_file="${ROOT_DIR}/tools/ci/policies/data/naming.json"
  python3 "${ROOT_DIR}/tools/ci/bin/read_json_field.py" --file "$ssot_file" --field "$field"
}

run_policy_runner_bridge() {
  local policy_check_id="$1"
  local policy_out_dir="$2"
  local summary_message="$3"
  local fallback_evidence="$4"

  mkdir -p "${ROOT_DIR}/${policy_out_dir}"

  local policy_exit=0
  if ! ci_run_capture "${summary_message}" dotnet "${ROOT_DIR}/tools/ci/checks/PolicyRunner/bin/Release/net10.0/PolicyRunner.dll" --check-id "${policy_check_id}" --repo-root "${ROOT_DIR}" --out-dir "${policy_out_dir}"; then
    policy_exit=1
  fi

  local policy_result_json="${ROOT_DIR}/${policy_out_dir}/result.json"
  if [[ ! -f "${policy_result_json}" ]]; then
    ci_result_add_violation "CI-POLICY-001" "fail" "PolicyRunner did not produce result.json" "${policy_out_dir}/result.json"
    ci_result_append_summary "${summary_message} failed (missing result.json)."
    return 1
  fi

  local findings=0
  local has_fail=0
  while IFS= read -r violation; do
    local rule_id severity message
    rule_id="$(jq -r '.rule_id' <<< "$violation")"
    severity="$(jq -r '.severity' <<< "$violation")"
    message="$(jq -r '.message' <<< "$violation")"

    mapfile -t evidence_paths < <(jq -r '.evidence_paths[]' <<< "$violation")
    if [[ "${#evidence_paths[@]}" -eq 0 ]]; then
      evidence_paths=("${fallback_evidence}")
    fi

    ci_result_add_violation "$rule_id" "$severity" "$message" "${evidence_paths[@]}"
    findings=$((findings + 1))
    if [[ "$severity" == "fail" ]]; then
      has_fail=1
    fi
  done < <(jq -c '.rule_violations[]' "${policy_result_json}")

  if [[ "$findings" -eq 0 ]]; then
    ci_result_append_summary "${summary_message} passed."
  else
    ci_result_append_summary "${summary_message} violations: ${findings}"
  fi

  if [[ "$policy_exit" -ne 0 || "$has_fail" -eq 1 ]]; then
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
  run_or_fail "CI-PREFLIGHT-001" "PR governance checks" bash "${ROOT_DIR}/tools/ci/check-pr-governance.sh"
  run_or_fail "CI-PREFLIGHT-001" "Code scanning tools open alerts must be zero" bash "${ROOT_DIR}/tools/ci/check-code-scanning-tools-zero.sh"
  run_or_fail "CI-CODEQL-001" "CodeQL default setup must be not-configured" bash "${ROOT_DIR}/tools/ci/check-codeql-default-setup.sh"
  run_or_fail "CI-PREFLIGHT-001" "Doc consistency drift guard" python3 "${ROOT_DIR}/tools/check-doc-consistency.py"
  run_or_fail "CI-PREFLIGHT-001" "Doc shell compatibility guard" python3 "${ROOT_DIR}/tools/check-doc-shell-compat.py"
  run_or_fail "CI-PREFLIGHT-001" "PR scope guard" python3 "${ROOT_DIR}/tools/ci/check-pr-scope.py" --repo-root "${ROOT_DIR}" --manifest "${ROOT_DIR}/tools/ci/policies/data/pr_scope_allowlist.txt" --base-ref "${PR_SCOPE_BASE_REF:-origin/main}" --mode combined --out "${ROOT_DIR}/${OUT_DIR}/pr-scope-summary.json"
  run_or_fail "CI-PREFLIGHT-001" "Docs checker unit tests" python3 -m unittest discover -s "${ROOT_DIR}/tools/tests" -p "test_*.py" -v
  run_or_fail "CI-PREFLIGHT-001" "Policy/RoC bijection" python3 "${ROOT_DIR}/tools/check-policy-roc.py" --out "${ROOT_DIR}/artifacts/policy_roc_matrix.tsv"
  run_or_fail "CI-PREFLIGHT-001" "VB Dim alignment check" python3 "${ROOT_DIR}/tools/ci/check-vb-dim-alignment.py" --root "${ROOT_DIR}/src/FileTypeDetection"
  run_or_fail "CI-PREFLIGHT-001" "Format check (style)" dotnet format "${ROOT_DIR}/FileClassifier.sln" style --verify-no-changes
  run_or_fail "CI-PREFLIGHT-001" "Format check (analyzers)" dotnet format "${ROOT_DIR}/FileClassifier.sln" analyzers --verify-no-changes
  if ! run_policy_runner_bridge "preflight" "artifacts/ci/_policy_preflight" "Policy shell safety" "tools/ci/bin/run.sh"; then
    return 1
  fi
  run_or_fail "CI-GRAPH-001" "CI graph assertion" bash "${ROOT_DIR}/tools/ci/bin/assert_ci_graph.sh"

  ci_result_append_summary "Preflight checks completed."
}

run_docs_links_full() {
  run_or_fail "CI-DOCS-LINKS-001" "Doc consistency drift guard" python3 "${ROOT_DIR}/tools/check-doc-consistency.py"
  run_or_fail "CI-DOCS-LINKS-001" "Full docs/link validation" python3 "${ROOT_DIR}/tools/check-docs.py"
  ci_result_append_summary "Docs links full validation completed."
}

run_build() {
  run_or_fail "CI-BUILD-001" "Restore solution (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/FileClassifier.sln" -v minimal
  run_or_fail "CI-BUILD-001" "Build solution" dotnet build "${ROOT_DIR}/FileClassifier.sln" --no-restore -warnaserror -v minimal
  ci_result_append_summary "Build completed."
}

run_api_contract() {
  run_or_fail "CI-CONTRACT-001" "Restore test project (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj" -v minimal
  run_or_fail "CI-CONTRACT-001" "Run API contract tests" dotnet test "${ROOT_DIR}/tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj" -c Release --no-restore --filter "Category=ApiContract" -v minimal
  ci_result_append_summary "API contract checks completed."
}

run_pack() {
  local package_project="${ROOT_DIR}/src/FileTypeDetection/FileTypeDetectionLib.vbproj"
  local pack_output_dir="${ROOT_DIR}/${OUT_DIR}/nuget"
  local expected_package_id
  expected_package_id="$(read_naming_ssot_field package_id)"
  if [[ -z "${expected_package_id}" ]]; then
    ci_result_add_violation "CI-PACK-001" "fail" "Naming SSOT package_id is missing." "tools/ci/policies/data/naming.json"
    return 1
  fi

  mkdir -p "${pack_output_dir}"
  run_or_fail "CI-PACK-001" "Restore package project (locked mode)" dotnet restore --locked-mode "${package_project}" -v minimal
  run_or_fail "CI-PACK-001" "Build package project" dotnet build "${package_project}" -c Release --no-restore -warnaserror -v minimal
  run_or_fail "CI-PACK-001" "Pack package project" dotnet pack "${package_project}" -c Release --no-build -o "${pack_output_dir}" -v minimal

  local nupkg_path
  nupkg_path="$(find_pack_nupkg "${pack_output_dir}")"
  if [[ -z "${nupkg_path}" ]]; then
    ci_result_add_violation "CI-PACK-001" "fail" "No nupkg produced by pack job." "${pack_output_dir}"
    return 1
  fi

  local package_id package_version
  package_id="$(read_nupkg_metadata "${nupkg_path}" "id")"
  package_version="$(read_nupkg_metadata "${nupkg_path}" "version")"
  if [[ -z "${package_id}" || -z "${package_version}" ]]; then
    ci_result_add_violation "CI-PACK-001" "fail" "Unable to read nupkg metadata (id/version)." "${nupkg_path}"
    return 1
  fi

  if [[ "${package_id}" != "${expected_package_id}" ]]; then
    ci_result_add_violation "CI-PACK-001" "fail" "Unexpected package id '${package_id}' (expected '${expected_package_id}')." "${nupkg_path}"
    return 1
  fi

  if ! unzip -l "${nupkg_path}" | rg -q "lib/.*/FileClassifier\\.CSCore\\.dll"; then
    ci_result_add_violation "CI-PACK-001" "fail" "Single-package contract violated: FileClassifier.CSCore.dll missing in nupkg." "${nupkg_path}"
    return 1
  fi

  printf '%s\n' "${nupkg_path}" > "${ROOT_DIR}/${OUT_DIR}/nupkg-path.txt"
  printf '%s\n' "${package_id}" > "${ROOT_DIR}/${OUT_DIR}/package-id.txt"
  printf '%s\n' "${package_version}" > "${ROOT_DIR}/${OUT_DIR}/package-version.txt"
  ci_result_append_summary "Pack completed (${package_id} ${package_version})."
}

run_naming_snt() {
  local naming_summary="${ROOT_DIR}/${OUT_DIR}/naming-snt-summary.json"
  build_validators
  run_or_fail "CI-NAMING-001" "Naming SNT checker" bash "${ROOT_DIR}/tools/ci/check-naming-snt.sh" --repo-root "${ROOT_DIR}" --ssot "${ROOT_DIR}/tools/ci/policies/data/naming.json" --out "${naming_summary}"
  if ! run_policy_runner_bridge "naming-snt" "artifacts/ci/_policy_naming_snt" "Policy naming SNT" "${OUT_DIR}/naming-snt-summary.json"; then
    return 1
  fi
  ci_result_append_summary "Naming SNT checks completed."
}

run_versioning_svt() {
  local versioning_summary="${ROOT_DIR}/${OUT_DIR}/versioning-svt-summary.json"
  build_validators
  run_or_fail "CI-VERSION-001" "Versioning SVT checker" bash "${ROOT_DIR}/tools/ci/check-versioning-svt.sh" --repo-root "${ROOT_DIR}" --naming-ssot "${ROOT_DIR}/tools/ci/policies/data/naming.json" --versioning-ssot "${ROOT_DIR}/tools/ci/policies/data/versioning.json" --out "${versioning_summary}"
  if ! run_policy_runner_bridge "versioning-svt" "artifacts/ci/_policy_versioning_svt" "Policy versioning SVT" "${OUT_DIR}/versioning-svt-summary.json"; then
    return 1
  fi
  ci_result_append_summary "Versioning SVT checks completed."
}

run_version_convergence() {
  local remote_required="${REQUIRE_REMOTE:-0}"
  run_or_fail "CI-VERSION-002" "Version convergence (repo/docs/package, require_remote=${remote_required})" env ROOT_DIR="${ROOT_DIR}" OUT_DIR="${OUT_DIR}" REQUIRE_REMOTE="${remote_required}" bash "${ROOT_DIR}/tools/versioning/verify-version-convergence.sh"
  ci_result_append_summary "Version convergence checks completed."
}

run_consumer_smoke() {
  local pack_out_dir="${ROOT_DIR}/artifacts/ci/pack/nuget"
  local consumer_project="${ROOT_DIR}/samples/PortableConsumer/PortableConsumer.csproj"
  local consumer_nuget_config="${ROOT_DIR}/samples/PortableConsumer/NuGet.config"

  if [[ ! -f "${consumer_project}" ]]; then
    ci_result_add_violation "CI-SMOKE-001" "fail" "Consumer project is missing." "${consumer_project}"
    return 1
  fi

  if rg -n "<ProjectReference" "${consumer_project}" >/dev/null 2>&1; then
    ci_result_add_violation "CI-SMOKE-001" "fail" "Consumer project must not reference source projects." "${consumer_project}"
    return 1
  fi

  local nupkg_path package_version
  nupkg_path="$(find_pack_nupkg "${pack_out_dir}")"
  if [[ -z "${nupkg_path}" ]]; then
    ci_result_add_violation "CI-SMOKE-001" "fail" "No nupkg available for consumer smoke." "${pack_out_dir}"
    return 1
  fi
  package_version="$(read_nupkg_metadata "${nupkg_path}" "version")"
  if [[ -z "${package_version}" ]]; then
    ci_result_add_violation "CI-SMOKE-001" "fail" "Unable to read package version from nupkg." "${nupkg_path}"
    return 1
  fi

  run_or_fail "CI-SMOKE-001" "Restore consumer sample from package" dotnet restore "${consumer_project}" --source "${pack_out_dir}" --source "https://api.nuget.org/v3/index.json" -p:PortableConsumerPackageVersion="${package_version}" -p:RestoreLockedMode=false --force-evaluate -v minimal
  run_or_fail "CI-SMOKE-001" "Build consumer sample from package" dotnet build "${consumer_project}" -c Release --no-restore -p:PortableConsumerPackageVersion="${package_version}" -v minimal
  run_or_fail "CI-SMOKE-001" "Run consumer sample from package" dotnet run --project "${consumer_project}" -c Release -f net10.0 --no-build -p:PortableConsumerPackageVersion="${package_version}"
  ci_result_append_summary "Consumer smoke completed against package ${package_version}."
}

run_package_backed_tests() {
  local pack_out_dir="${ROOT_DIR}/artifacts/ci/pack/nuget"
  local package_tests_project="${ROOT_DIR}/tests/PackageBacked.Tests/PackageBacked.Tests.csproj"
  local package_tests_nuget_config="${ROOT_DIR}/tests/PackageBacked.Tests/NuGet.config"

  local nupkg_path package_version
  nupkg_path="$(find_pack_nupkg "${pack_out_dir}")"
  if [[ -z "${nupkg_path}" ]]; then
    ci_result_add_violation "CI-PKGTEST-001" "fail" "No nupkg available for package-backed tests." "${pack_out_dir}"
    return 1
  fi
  package_version="$(read_nupkg_metadata "${nupkg_path}" "version")"
  if [[ -z "${package_version}" ]]; then
    ci_result_add_violation "CI-PKGTEST-001" "fail" "Unable to read package version from nupkg." "${nupkg_path}"
    return 1
  fi

  run_or_fail "CI-PKGTEST-001" "Restore package-backed tests from package" dotnet restore "${package_tests_project}" --source "${pack_out_dir}" --source "https://api.nuget.org/v3/index.json" -p:PackageBackedVersion="${package_version}" -p:RestoreLockedMode=false --force-evaluate -v minimal

  if dotnet --list-runtimes | grep -q "Microsoft.NETCore.App 8\\."; then
    run_or_fail "CI-PKGTEST-001" "Run package-backed tests" dotnet test "${package_tests_project}" -c Release --no-restore -p:PackageBackedVersion="${package_version}" -v minimal
  else
    if [[ "${CI:-}" == "true" ]]; then
      ci_result_add_violation "CI-PKGTEST-001" "fail" "Missing .NET 8 runtime for package-backed tests in CI." "${CI_RAW_LOG}"
      return 1
    fi
    run_or_fail "CI-PKGTEST-001" "Run package-backed tests (net10 fallback outside CI)" dotnet test "${package_tests_project}" -c Release -f net10.0 --no-restore -p:PackageBackedVersion="${package_version}" -v minimal
  fi

  ci_result_append_summary "Package-backed tests completed against package ${package_version}."
}

run_security_nuget() {
  run_or_fail "CI-SECURITY-002" "Restore solution (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/FileClassifier.sln" -v minimal
  run_or_fail "CI-SECURITY-002" "NuGet vulnerability scan" dotnet list "${ROOT_DIR}/FileClassifier.sln" package --vulnerable --include-transitive

  if log_contains_high_or_critical; then
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
  local coverage_assembly
  local coverage_include
  mkdir -p "$tests_dir" "$coverage_dir"

  coverage_assembly="$(read_naming_ssot_field assembly_name)"
  if [[ -z "${coverage_assembly}" ]]; then
    ci_result_add_violation "CI-TEST-001" "fail" "Naming SSOT assembly_name is missing." "tools/ci/policies/data/naming.json"
    return 1
  fi
  coverage_include="[${coverage_assembly}]*"

  run_or_fail "CI-TEST-001" "Restore solution (locked mode)" dotnet restore --locked-mode "${ROOT_DIR}/FileClassifier.sln" -v minimal
  run_or_fail "CI-TEST-001" "BDD tests + coverage" env TEST_BDD_OUTPUT_DIR="$tests_dir" bash "${ROOT_DIR}/tools/test-bdd-readable.sh" -- /p:CollectCoverage=true /p:Include="${coverage_include}" /p:CoverletOutputFormat=cobertura /p:CoverletOutput="${coverage_dir}/coverage" /p:Threshold=85%2c69 /p:ThresholdType=line%2cbranch /p:ThresholdStat=total
  ci_result_append_summary "BDD coverage checks completed."
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

  local version_required
  if ! version_required="$(env MODE=required BASE_REF=origin/main HEAD_REF="$head_sha" "${ROOT_DIR}/tools/versioning/check-versioning.sh")"; then
    ci_result_add_violation "CI-LABEL-001" "fail" "Failed to derive required versioning decision." "$CI_RAW_LOG"
    return 1
  fi
  if [[ -z "${version_required}" ]]; then
    ci_result_add_violation "CI-LABEL-001" "fail" "Required versioning decision is empty." "$CI_RAW_LOG"
    return 1
  fi
  ci_run_capture "Derive required versioning decision" bash -lc "printf 'required=%s\n' '${version_required}'"

  local files_json labels_json pr_title
  if ! files_json="$(gh_retry python3 "${ROOT_DIR}/tools/ci/bin/github_api.py" pr-files --repo "${GITHUB_REPOSITORY}" --pr "${pr_number}")"; then
    ci_result_add_violation "CI-LABEL-001" "fail" "Failed to read PR files from GitHub API." "$CI_RAW_LOG"
    return 1
  fi
  if ! labels_json="$(gh_retry python3 "${ROOT_DIR}/tools/ci/bin/github_api.py" issue-labels --repo "${GITHUB_REPOSITORY}" --issue "${pr_number}")"; then
    ci_result_add_violation "CI-LABEL-001" "fail" "Failed to read PR labels from GitHub API." "$CI_RAW_LOG"
    return 1
  fi
  if ! pr_title="$(gh_retry python3 "${ROOT_DIR}/tools/ci/bin/github_api.py" pr-title --repo "${GITHUB_REPOSITORY}" --pr "${pr_number}")"; then
    ci_result_add_violation "CI-LABEL-001" "fail" "Failed to read PR title from GitHub API." "$CI_RAW_LOG"
    return 1
  fi

  mkdir -p "${OUT_DIR}"
  FILES_JSON="$files_json" EXISTING_LABELS_JSON="$labels_json" PR_TITLE="$pr_title" VERSION_REQUIRED="${version_required}" VERSION_ACTUAL="none" VERSION_REASON="contract-run" VERSION_GUARD_EXIT="0" OUTPUT_PATH="${OUT_DIR}/decision.json" \
    ci_run_capture "Compute deterministic labels" node "${ROOT_DIR}/tools/versioning/compute-pr-labels.js"

  run_or_fail "CI-LABEL-001" "Validate label decision" node "${ROOT_DIR}/tools/versioning/validate-label-decision.js" "${ROOT_DIR}/tools/versioning/label-schema.json" "${OUT_DIR}/decision.json"
  local expected_json actual_json put_payload_path
  expected_json="$(jq -cn \
    --argjson existing "${labels_json}" \
    --argjson add "$(jq -c '.labels_to_add // []' "${OUT_DIR}/decision.json")" \
    --argjson remove "$(jq -c '.labels_to_remove // []' "${OUT_DIR}/decision.json")" \
    '$existing
      | map(select(. as $label | ($remove | index($label) | not)))
      | . + $add
      | unique
      | sort')"
  put_payload_path="${OUT_DIR}/labels-put.json"
  jq -cn --argjson labels "${expected_json}" '{labels:$labels}' > "${put_payload_path}"

  run_or_fail "CI-LABEL-001" "Apply labels (single deterministic PUT)" gh_retry python3 "${ROOT_DIR}/tools/ci/bin/github_api.py" put-issue-labels --repo "${GITHUB_REPOSITORY}" --issue "${pr_number}" --payload "${put_payload_path}"

  if ! actual_json="$(gh_retry python3 "${ROOT_DIR}/tools/ci/bin/github_api.py" issue-labels --repo "${GITHUB_REPOSITORY}" --issue "${pr_number}" --sort)"; then
    ci_result_add_violation "CI-LABEL-001" "fail" "Failed to re-read PR labels after apply." "$CI_RAW_LOG"
    return 1
  fi
  if [[ "${actual_json}" != "${expected_json}" ]]; then
    ci_result_add_violation "CI-LABEL-001" "fail" "Applied labels do not match decision.json." "${OUT_DIR}/decision.json" "${CI_RAW_LOG}"
    ci_result_append_summary "PR labeling apply failed verification."
    return 1
  fi

  ci_run_capture "Post-apply labels confirmation" gh_retry python3 "${ROOT_DIR}/tools/ci/bin/github_api.py" issue-labels --repo "${GITHUB_REPOSITORY}" --issue "${pr_number}" --sort
  ci_result_append_summary "PR labeling checks completed."
}

run_qodana_contract() {
  build_validators
  local sarif_path="${OUT_DIR}/qodana.sarif.json"
  local filtered_sarif_path="${OUT_DIR}/qodana.upload.sarif.json"
  if ! ci_run_capture "Qodana contract validator" dotnet "${ROOT_DIR}/tools/ci/checks/QodanaContractValidator/bin/Release/net10.0/QodanaContractValidator.dll" --sarif "$sarif_path" --filtered-sarif-out "$filtered_sarif_path"; then
    if log_contains_code "CI-QODANA-001"; then
      ci_result_add_violation "CI-QODANA-001" "fail" "QODANA_TOKEN missing" "$CI_RAW_LOG"
    elif log_contains_code "CI-QODANA-002"; then
      ci_result_add_violation "CI-QODANA-002" "fail" "Qodana SARIF missing" "$CI_RAW_LOG"
    elif log_contains_code "CI-QODANA-003"; then
      ci_result_add_violation "CI-QODANA-003" "fail" "Qodana SARIF invalid" "$CI_RAW_LOG"
    elif log_contains_code "CI-QODANA-004"; then
      ci_result_add_violation "CI-QODANA-004" "fail" "Qodana blocking findings detected (High+)" "$CI_RAW_LOG"
    elif log_contains_code "CI-QODANA-005"; then
      ci_result_add_violation "CI-QODANA-005" "fail" "Qodana toolset/environment errors detected" "$CI_RAW_LOG"
    else
      ci_result_add_violation "CI-QODANA-001" "fail" "Qodana contract validation failed" "$CI_RAW_LOG"
    fi
    return 1
  fi
  if [[ ! -f "${filtered_sarif_path}" ]]; then
    ci_result_add_violation "CI-QODANA-003" "fail" "Filtered SARIF output missing" "$CI_RAW_LOG" "${filtered_sarif_path}"
    return 1
  fi
  ci_result_append_summary "Qodana contract validation completed."
}

run_policy_contract() {
  build_validators
  if ! run_policy_runner_bridge "$CHECK_ID" "$OUT_DIR" "Policy contract check (${CHECK_ID})" "tools/ci/policies/rules"; then
    return 1
  fi
  ci_result_append_summary "Policy contract check '${CHECK_ID}' completed."
}

main() {
  cd "$ROOT_DIR"
  case "$CHECK_ID" in
    preflight) run_preflight ;;
    docs-links-full) run_docs_links_full ;;
    api-contract) run_api_contract ;;
    pack) run_pack ;;
    naming-snt) run_naming_snt ;;
    versioning-svt) run_versioning_svt ;;
    version-convergence) run_version_convergence ;;
    consumer-smoke) run_consumer_smoke ;;
    package-backed-tests) run_package_backed_tests ;;
    build) run_build ;;
    security-nuget) run_security_nuget ;;
    tests-bdd-coverage) run_tests_bdd_coverage ;;
    summary|artifact_contract) run_policy_contract ;;
    pr-labeling) run_pr_labeling ;;
    qodana) run_qodana_contract ;;
    *)
      ci_result_add_violation "CI-RUNNER-001" "fail" "unknown check_id '${CHECK_ID}'" "tools/ci/bin/run.sh"
      return 2
      ;;
  esac
}

main

if [[ "$finalized" -eq 0 ]]; then
  ci_result_finalize
  finalized=1
fi

if [[ "$(cat "$CI_STATUS_FILE")" == "fail" ]]; then
  ci_result_append_summary "Check '${CHECK_ID}' failed."
  exit 1
fi

ci_result_append_summary "Check '${CHECK_ID}' passed."
