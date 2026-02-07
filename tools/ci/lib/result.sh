#!/usr/bin/env bash
set -euo pipefail

# shellcheck source=tools/ci/lib/log.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/log.sh"

ci_result_init() {
  local check_id="$1"
  local out_dir="$2"

  export CI_CHECK_ID="$check_id"
  export CI_OUT_DIR="$out_dir"
  export CI_RAW_LOG="$out_dir/raw.log"
  export CI_SUMMARY_MD="$out_dir/summary.md"
  export CI_RESULT_JSON="$out_dir/result.json"
  export CI_VIOLATIONS_NDJSON="$out_dir/.violations.ndjson"
  export CI_EVIDENCE_NDJSON="$out_dir/.evidence.ndjson"
  export CI_STATUS_FILE="$out_dir/.status"
  export CI_START_MS
  export CI_START_AT

  mkdir -p "$out_dir"
  : > "$CI_RAW_LOG"
  : > "$CI_SUMMARY_MD"
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
  status="$(cat "$CI_STATUS_FILE")"
  duration_ms=$((finished_ms - CI_START_MS))

  local violations_json evidence_json artifacts_json
  violations_json=$(jq -s . "$CI_VIOLATIONS_NDJSON")
  evidence_json=$(jq -s 'unique' "$CI_EVIDENCE_NDJSON")
  artifacts_json=$(jq -cn --arg raw "$CI_RAW_LOG" --arg summary "$CI_SUMMARY_MD" --arg result "$CI_RESULT_JSON" '[ $raw, $summary, $result ]')

  jq -cn \
    --arg check_id "$CI_CHECK_ID" \
    --arg status "$status" \
    --arg started_at "$CI_START_AT" \
    --arg finished_at "$finished_at" \
    --argjson duration_ms "$duration_ms" \
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

ci_run_capture() {
  local description="$1"
  shift

  ci_info "$description"
  {
    printf '$ %s\n' "$*"
    "$@"
  } >> "$CI_RAW_LOG" 2>&1
}
