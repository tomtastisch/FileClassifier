#!/usr/bin/env bash
set -euo pipefail

ci_now_utc() {
  date -u +"%Y-%m-%dT%H:%M:%SZ"
}

ci_now_ms() {
  local ms
  ms=$(date -u +%s%3N 2>/dev/null || true)
  if [[ -n "$ms" && "$ms" =~ ^[0-9]+$ ]]; then
    printf '%s\n' "$ms"
    return 0
  fi
  printf '%s000\n' "$(date -u +%s)"
}

ci_log() {
  local level="$1"
  shift
  printf '[%s] [%s] %s\n' "$(ci_now_utc)" "$level" "$*"
}

ci_info() { ci_log INFO "$@"; }
ci_warn() { ci_log WARN "$@"; }
ci_error() { ci_log ERROR "$@" >&2; }
