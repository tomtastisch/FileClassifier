#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd -- "${SCRIPT_DIR}/../../.." && pwd)"
PACKAGE_FILE="${ROOT_DIR}/Directory.Packages.props"
CONFIG_FILE="${ROOT_DIR}/tools/ci/policies/data/dependency_canary.json"
TEST_PROJECT="${ROOT_DIR}/tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj"

log() {
  printf '%s\n' "$*"
}

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

require_cmd() {
  local cmd="$1"
  command -v "$cmd" >/dev/null 2>&1 || fail "required command not found: ${cmd}"
}

main() {
  local dependency="${1:-SharpCompress}"
  local requested="${2:-latest}"
  local prepare_out
  local current_version=""
  local target_version=""
  local updated=""
  local test_filter=""

  require_cmd python3
  require_cmd dotnet

  prepare_out="$(
    python3 "${ROOT_DIR}/tools/ci/bin/dependency_canary.py" prepare \
      --dependency "${dependency}" \
      --requested "${requested}" \
      --config "${CONFIG_FILE}" \
      --packages-file "${PACKAGE_FILE}"
  )"

  while IFS='=' read -r key value; do
    case "${key}" in
      CURRENT_VERSION) current_version="${value}" ;;
      TARGET_VERSION) target_version="${value}" ;;
      UPDATED) updated="${value}" ;;
      TEST_FILTER) test_filter="${value}" ;;
    esac
  done <<< "${prepare_out}"

  [[ -n "${current_version}" && -n "${target_version}" && -n "${test_filter}" ]] || fail "invalid prepare output"

  log "INFO: Dependency=${dependency} current=${current_version} target=${target_version} updated=${updated}"
  dotnet restore "${ROOT_DIR}/FileClassifier.sln" -v minimal
  dotnet test "${TEST_PROJECT}" -c Release -v minimal --filter "${test_filter}"
}

main "$@"
