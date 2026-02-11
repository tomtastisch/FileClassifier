#!/usr/bin/env bash
set -euo pipefail

release_version="${1:-}"
verify_log_path="${2:-}"
package_id="${3:-Tomtastisch.FileClassifier}"

if [[ -z "${release_version}" || -z "${verify_log_path}" || -z "${package_id}" ]]; then
  echo "Usage: $0 <release_version> <verify_log_path> [package_id]" >&2
  exit 1
fi

mkdir -p "$(dirname -- "${verify_log_path}")"

EXPECTED_VERSION="${release_version}" \
PKG_ID="${package_id}" \
PKG_VER="${release_version}" \
VERIFY_ONLINE=1 \
REQUIRE_SEARCH=1 \
REQUIRE_REGISTRATION=1 \
REQUIRE_FLATCONTAINER=1 \
RETRY_COUNT=179 \
RETRY_SLEEP_SECONDS=10 \
bash tools/ci/verify_nuget_release.sh 2>&1 | tee "${verify_log_path}"
