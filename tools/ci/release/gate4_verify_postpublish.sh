#!/usr/bin/env bash
set -euo pipefail

expected_version="${1:?expected version required}"
nupkg_path="${2:?nupkg path required}"
retry_count="${SVT_POSTPUBLISH_RETRY_COUNT:-59}"
retry_sleep_seconds="${SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS:-10}"

EXPECTED_VERSION="${expected_version}" \
NUPKG_PATH="${nupkg_path}" \
RETRY_COUNT="${retry_count}" \
RETRY_SLEEP_SECONDS="${retry_sleep_seconds}" \
bash tools/ci/verify_nuget_release.sh
