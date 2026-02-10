#!/usr/bin/env bash
set -euo pipefail

# shellcheck source=tools/ci/lib/log.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/log.sh"

ci_result_init() {
  local check_id="$1"
  local out_dir="$2"
  local lib_dir

  export CI_CHECK_ID="$check_id"
  export CI_OUT_DIR="$out_dir"
  export CI_RAW_LOG="$out_dir/raw.log"
  export CI_SUMMARY_MD="$out_dir/summary.md"
  export CI_RESULT_JSON="$out_dir/result.json"
  export CI_DIAG_JSON="$out_dir/diag.json"
  export CI_VIOLATIONS_NDJSON="$out_dir/.violations.ndjson"
  export CI_EVIDENCE_NDJSON="$out_dir/.evidence.ndjson"
  export CI_STATUS_FILE="$out_dir/.status"
  export CI_ARTIFACT_NAME="${CI_ARTIFACT_NAME:-ci-${check_id}}"
  export CI_RUN_URL="${GITHUB_SERVER_URL:-https://github.com}/${GITHUB_REPOSITORY:-}/${GITHUB_RUN_ID:+actions/runs/${GITHUB_RUN_ID}}"
  lib_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
  export CI_ERROR_RENDERER="${lib_dir}/error_ux.py"
  export CI_START_MS
  export CI_START_AT

  mkdir -p "$out_dir"
  : > "$CI_RAW_LOG"
  : > "$CI_SUMMARY_MD"
  printf '{}\n' > "$CI_DIAG_JSON"
  : > "$CI_VIOLATIONS_NDJSON"
  : > "$CI_EVIDENCE_NDJSON"
  printf 'pass' > "$CI_STATUS_FILE"

  CI_START_MS="$(ci_now_ms)"
  CI_START_AT="$(ci_now_utc)"
}

ci_result_append_summary() {
  printf '%s\n' "$*" >> "$CI_SUMMARY_MD"
}

ci_result_add_evidence() {
  local evidence_path="$1"
  jq -cn --arg p "$evidence_path" '$p' >> "$CI_EVIDENCE_NDJSON"
}

ci_result_add_violation() {
  local rule_id="$1"
  local severity="$2"
  local message="$3"
  shift 3
  local evidence_paths=("$@")

  if [[ "$severity" == "fail" ]]; then
    printf 'fail' > "$CI_STATUS_FILE"
  elif [[ "$(cat "$CI_STATUS_FILE")" == "pass" ]]; then
    printf 'warn' > "$CI_STATUS_FILE"
  fi

  local ev_json
  ev_json=$(printf '%s\n' "${evidence_paths[@]}" | jq -R . | jq -s .)

  jq -cn \
    --arg rule_id "$rule_id" \
    --arg severity "$severity" \
    --arg message "$message" \
    --argjson evidence_paths "$ev_json" \
    '{rule_id:$rule_id,severity:$severity,message:$message,evidence_paths:$evidence_paths}' >> "$CI_VIOLATIONS_NDJSON"

  local p
  for p in "${evidence_paths[@]}"; do
    ci_result_add_evidence "$p"
  done
}

ci_result_finalize() {
  local finished_ms finished_at duration_ms status
  finished_ms="$(ci_now_ms)"
  finished_at="$(ci_now_utc)"
  duration_ms=$((finished_ms - CI_START_MS))

  ci_result_write_json() {
    local write_status="$1"
    local write_finished_at="$2"
    local write_duration_ms="$3"
    local violations_json evidence_json artifacts_json
    violations_json=$(jq -s . "$CI_VIOLATIONS_NDJSON")
    evidence_json=$(jq -s 'unique' "$CI_EVIDENCE_NDJSON")
    artifacts_json=$(jq -cn --arg raw "$CI_RAW_LOG" --arg summary "$CI_SUMMARY_MD" --arg result "$CI_RESULT_JSON" --arg diag "$CI_DIAG_JSON" '[ $raw, $summary, $result, $diag ]')

    jq -cn \
      --arg check_id "$CI_CHECK_ID" \
      --arg status "$write_status" \
      --arg started_at "$CI_START_AT" \
      --arg finished_at "$write_finished_at" \
      --argjson duration_ms "$write_duration_ms" \
      --argjson rule_violations "$violations_json" \
      --argjson evidence_paths "$evidence_json" \
      --argjson artifacts "$artifacts_json" \
      '{
        schema_version: 1,
        check_id: $check_id,
        status: $status,
        rule_violations: $rule_violations,
        evidence_paths: $evidence_paths,
        artifacts: $artifacts,
        timing: {
          started_at: $started_at,
          finished_at: $finished_at,
          duration_ms: $duration_ms
        }
      }' > "$CI_RESULT_JSON"
  }

  ci_result_validate_schema() {
    local lib_dir repo_root schema_path validator_project validator_dll
    lib_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
    repo_root="$(cd -- "${lib_dir}/../../.." && pwd)"
    schema_path="${repo_root}/tools/ci/schema/result.schema.json"
    validator_project="${repo_root}/tools/ci/checks/ResultSchemaValidator/ResultSchemaValidator.csproj"
    validator_dll="${repo_root}/tools/ci/checks/ResultSchemaValidator/bin/Release/net10.0/ResultSchemaValidator.dll"

    if [[ ! -f "$schema_path" ]]; then
      ci_result_add_violation "CI-SCHEMA-001" "fail" "result schema missing" "$schema_path"
      return 1
    fi

    if [[ ! -f "$validator_project" ]]; then
      ci_result_add_violation "CI-SCHEMA-001" "fail" "ResultSchemaValidator project missing" "$validator_project"
      return 1
    fi

    if [[ ! -f "$validator_dll" ]]; then
      {
        printf '$ dotnet restore --locked-mode %s\n' "$validator_project"
        dotnet restore --locked-mode "$validator_project"
        printf '$ dotnet build -c Release %s\n' "$validator_project"
        dotnet build -c Release "$validator_project"
      } >> "$CI_RAW_LOG" 2>&1 || {
        ci_result_add_violation "CI-SCHEMA-001" "fail" "ResultSchemaValidator build failed" "$CI_RAW_LOG"
        return 1
      }
    fi

    {
      printf '$ dotnet %s --schema %s --result %s\n' "$validator_dll" "$schema_path" "$CI_RESULT_JSON"
      dotnet "$validator_dll" --schema "$schema_path" --result "$CI_RESULT_JSON"
    } >> "$CI_RAW_LOG" 2>&1 || {
      ci_result_add_violation "CI-SCHEMA-001" "fail" "result.json schema validation failed" "$CI_RESULT_JSON" "$CI_RAW_LOG"
      return 1
    }
  }

  status="$(cat "$CI_STATUS_FILE")"
  ci_result_write_json "$status" "$finished_at" "$duration_ms"
  ci_result_validate_schema || true
  status="$(cat "$CI_STATUS_FILE")"
  ci_result_write_json "$status" "$finished_at" "$duration_ms"

  if [[ "$status" == "fail" ]]; then
    ci_emit_error_ux
  fi
}

ci_run_capture() {
  local description="$1"
  shift

  {
    printf '# %s\n' "$description"
    printf '$ %s\n' "$*"
    "$@"
  } >> "$CI_RAW_LOG" 2>&1
}

ci_map_error_keys() {
  local rule_id="$1"
  case "$rule_id" in
    CI-SETUP-*) echo "setup command_failed" ;;
    CI-SHELL-*|CI-DOCS-*|CI-NAMING-*|CI-VERSION-*|CI-ARTIFACT-*|CI-POLICY-*) echo "policy policy_violation" ;;
    CI-GRAPH-*) echo "policy command_failed" ;;
    CI-BUILD-*) echo "build command_failed" ;;
    CI-CONTRACT-*|CI-TEST-*|CI-PKGTEST-*|CI-SMOKE-*) echo "test command_failed" ;;
    CI-PACK-*) echo "pack command_failed" ;;
    CI-SECURITY-001) echo "security blocking_findings" ;;
    CI-SECURITY-*) echo "security command_failed" ;;
    CI-QODANA-001|CI-QODANA-002) echo "qodana missing_input" ;;
    CI-QODANA-003|CI-QODANA-005) echo "qodana command_failed" ;;
    CI-QODANA-004) echo "qodana blocking_findings" ;;
    CI-SCHEMA-*) echo "schema schema_failure" ;;
    CI-RUNNER-*) echo "runner missing_input" ;;
    *) echo "generic command_failed" ;;
  esac
}

ci_emit_error_ux() {
  local first_fail_json rule_id evidence_join map_out step_key class_key run_url
  first_fail_json="$(jq -sc 'map(select(.severity=="fail"))[0] // {}' "$CI_VIOLATIONS_NDJSON")"
  rule_id="$(jq -r '.rule_id // "CI-RUNNER-001"' <<<"$first_fail_json")"
  evidence_join="$(jq -r '(.evidence_paths // []) | join("|")' <<<"$first_fail_json")"
  map_out="$(ci_map_error_keys "$rule_id")"
  step_key="${map_out%% *}"
  class_key="${map_out##* }"
  run_url="$CI_RUN_URL"
  if [[ "$run_url" == *"//actions/runs/" || "$run_url" == "https://github.com/" ]]; then
    run_url="https://github.com/${GITHUB_REPOSITORY:-}"
  fi

  # Some workflows (e.g. version-policy) upload the per-check artifact after the check step runs.
  # In that case, artifact URL resolution would be a guaranteed false-negative pre-upload.
  # This flag defers artifact-link rendering to a post-upload verification step in the workflow.
  if [[ "${CI_DEFER_ARTIFACT_LINK_RESOLUTION:-}" == "1" ]]; then
    jq -cn \
      --arg check_id "$CI_CHECK_ID" \
      --arg artifact_name "$CI_ARTIFACT_NAME" \
      --arg run_url "$run_url" \
      --arg rule_id "$rule_id" \
      --arg ts "$(ci_now_utc)" \
      --arg msg "Artifact URL resolution deferred (CI_DEFER_ARTIFACT_LINK_RESOLUTION=1). Verify artifact existence after upload-artifact." \
      '{
        error_code:"deferred",
        check_id:$check_id,
        rule_id:$rule_id,
        artifact_name:$artifact_name,
        artifact_url:$run_url,
        message:$msg,
        timestamp_utc:$ts
      }' > "$CI_DIAG_JSON"
    printf 'INFO: %s\n' "$msg"
    return 0
  fi

  if ! python3 "$CI_ERROR_RENDERER" \
      --step-key "$step_key" \
      --class-key "$class_key" \
      --check-id "$CI_CHECK_ID" \
      --rule-id "$rule_id" \
      --artifact-name "$CI_ARTIFACT_NAME" \
      --run-url "$run_url" \
      --diag-path "$CI_DIAG_JSON" \
      --evidence-paths "$evidence_join"; then
    jq -cn \
      --arg check_id "$CI_CHECK_ID" \
      --arg artifact_name "$CI_ARTIFACT_NAME" \
      --arg run_url "$run_url" \
      --arg rule_id "$rule_id" \
      --arg ts "$(ci_now_utc)" \
      --arg msg "Error mapping/artifact-link failure for check '${CI_CHECK_ID}' (renderer_failed)." \
      '{
        error_code:"9901",
        step_id:"99",
        class_id:"01",
        check_id:$check_id,
        rule_id:$rule_id,
        artifact_name:$artifact_name,
        artifact_url:$run_url,
        message:$msg,
        timestamp_utc:$ts
      }' > "$CI_DIAG_JSON"
    printf '\033[31mERROR 9901: Error mapping/artifact-link failure for check %s (renderer_failed).\033[0m\n' "$CI_CHECK_ID"
    printf '\033[34mARTIFACT %s (artifact: %s)\033[0m\n' "$run_url" "$CI_ARTIFACT_NAME"
  fi
}
