#!/usr/bin/env bash
set -euo pipefail

expected_version="${1:?expected version required}"
nupkg_path="${2:?nupkg path required}"
is_prerelease="0"
if [[ "${expected_version}" == *-* ]]; then
  is_prerelease="1"
fi

# Stable NuGet registration can lag for tens of minutes after push; keep fail-closed
# and allow enough convergence time before declaring a hard failure (stable schedule
# below currently sums to ~29 minutes of potential wait time).
default_retry_schedule_stable="2,3,5,8,13,21,34,55,89,144,233,377,377,377"
default_retry_schedule_prerelease="2,3,5,8,13,21,34,55,89,144,233,377"
if [[ "${is_prerelease}" == "1" ]]; then
  retry_schedule_seconds="${SVT_POSTPUBLISH_RETRY_SCHEDULE_SECONDS:-${default_retry_schedule_prerelease}}"
else
  retry_schedule_seconds="${SVT_POSTPUBLISH_RETRY_SCHEDULE_SECONDS:-${default_retry_schedule_stable}}"
fi

if [[ -n "${SVT_POSTPUBLISH_RETRY_COUNT:-}" ]]; then
  retry_count="${SVT_POSTPUBLISH_RETRY_COUNT}"
else
  if [[ -n "${retry_schedule_seconds}" ]]; then
    retry_count="$(awk -F',' '{print NF}' <<<"${retry_schedule_seconds}")"
  else
    retry_count="59"
  fi
fi
retry_sleep_seconds="${SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS:-10}"

if [[ -n "${SVT_POSTPUBLISH_REQUIRE_REGISTRATION:-}" ]]; then
  require_registration="${SVT_POSTPUBLISH_REQUIRE_REGISTRATION}"
else
  # Registration endpoint is typically the slowest to converge and caused repeated
  # false negatives. Gate 4 now validates publish availability via V2 download.
  require_registration="0"
fi

if [[ -n "${SVT_POSTPUBLISH_REQUIRE_FLATCONTAINER:-}" ]]; then
  require_flatcontainer="${SVT_POSTPUBLISH_REQUIRE_FLATCONTAINER}"
else
  require_flatcontainer="0"
fi

if [[ -n "${SVT_POSTPUBLISH_REQUIRE_V2_DOWNLOAD:-}" ]]; then
  require_v2_download="${SVT_POSTPUBLISH_REQUIRE_V2_DOWNLOAD}"
else
  require_v2_download="1"
fi

if [[ ! "${retry_count}" =~ ^[0-9]+$ ]]; then
  echo "SVT_POSTPUBLISH_RETRY_COUNT must be a non-negative integer (actual='${retry_count}')" >&2
  exit 1
fi
if [[ ! "${retry_sleep_seconds}" =~ ^[0-9]+$ ]]; then
  echo "SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS must be a non-negative integer (actual='${retry_sleep_seconds}')" >&2
  exit 1
fi
if [[ -n "${retry_schedule_seconds}" && ! "${retry_schedule_seconds}" =~ ^[0-9]+(,[0-9]+)*$ ]]; then
  echo "SVT_POSTPUBLISH_RETRY_SCHEDULE_SECONDS must be comma-separated non-negative integers (actual='${retry_schedule_seconds}')" >&2
  exit 1
fi
if [[ "${require_registration}" != "0" && "${require_registration}" != "1" ]]; then
  echo "SVT_POSTPUBLISH_REQUIRE_REGISTRATION must be 0 or 1 (actual='${require_registration}')" >&2
  exit 1
fi
if [[ "${require_flatcontainer}" != "0" && "${require_flatcontainer}" != "1" ]]; then
  echo "SVT_POSTPUBLISH_REQUIRE_FLATCONTAINER must be 0 or 1 (actual='${require_flatcontainer}')" >&2
  exit 1
fi
if [[ "${require_v2_download}" != "0" && "${require_v2_download}" != "1" ]]; then
  echo "SVT_POSTPUBLISH_REQUIRE_V2_DOWNLOAD must be 0 or 1 (actual='${require_v2_download}')" >&2
  exit 1
fi

EXPECTED_VERSION="${expected_version}" \
NUPKG_PATH="${nupkg_path}" \
RETRY_COUNT="${retry_count}" \
RETRY_SLEEP_SECONDS="${retry_sleep_seconds}" \
RETRY_SCHEDULE_SECONDS="${retry_schedule_seconds}" \
REQUIRE_SEARCH=0 \
REQUIRE_REGISTRATION="${require_registration}" \
REQUIRE_FLATCONTAINER="${require_flatcontainer}" \
REQUIRE_V2_DOWNLOAD="${require_v2_download}" \
bash tools/ci/verify_nuget_release.sh
