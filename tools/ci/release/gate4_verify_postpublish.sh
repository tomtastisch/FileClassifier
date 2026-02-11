#!/usr/bin/env bash
set -euo pipefail

expected_version="${1:?expected version required}"
nupkg_path="${2:?nupkg path required}"
retry_count="${SVT_POSTPUBLISH_RETRY_COUNT:-59}"
retry_sleep_seconds="${SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS:-10}"

if [[ ! "${retry_count}" =~ ^[0-9]+$ ]]; then
  echo "SVT_POSTPUBLISH_RETRY_COUNT must be a non-negative integer (actual='${retry_count}')" >&2
  exit 1
fi
if [[ ! "${retry_sleep_seconds}" =~ ^[0-9]+$ ]]; then
  echo "SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS must be a non-negative integer (actual='${retry_sleep_seconds}')" >&2
  exit 1
fi

EXPECTED_VERSION="${expected_version}" \
NUPKG_PATH="${nupkg_path}" \
RETRY_COUNT="${retry_count}" \
RETRY_SLEEP_SECONDS="${retry_sleep_seconds}" \
REQUIRE_SEARCH=0 \
REQUIRE_REGISTRATION=1 \
REQUIRE_FLATCONTAINER=1 \
bash tools/ci/verify_nuget_release.sh
