#!/usr/bin/env bash
set -euo pipefail

release_version="${1:-}"
nupkg_path="${2:-}"
verify_log_path="${3:-}"

if [[ -z "${release_version}" || -z "${nupkg_path}" || -z "${verify_log_path}" ]]; then
  echo "Usage: $0 <release_version> <nupkg_path> <verify_log_path>" >&2
  exit 1
fi

EXPECTED_VERSION="${release_version}" \
NUPKG_PATH="${nupkg_path}" \
VERIFY_ONLINE=1 \
REQUIRE_SEARCH=1 \
REQUIRE_REGISTRATION=1 \
REQUIRE_FLATCONTAINER=1 \
RETRY_COUNT=179 \
RETRY_SLEEP_SECONDS=10 \
bash tools/ci/verify_nuget_release.sh | tee "${verify_log_path}"
