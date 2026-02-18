#!/usr/bin/env bash
set -euo pipefail

expected_version="${1:?expected version required}"
nupkg_path="${2:?nupkg path required}"
is_prerelease="0"
if [[ "${expected_version}" == *-* ]]; then
  is_prerelease="1"
fi

# Stable NuGet registration can lag several minutes after push; keep fail-closed
# but allow enough convergence time before declaring a hard failure.
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
  if [[ "${is_prerelease}" == "1" ]]; then
    require_registration="0"
  else
    require_registration="1"
  fi
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

EXPECTED_VERSION="${expected_version}" \
NUPKG_PATH="${nupkg_path}" \
RETRY_COUNT="${retry_count}" \
RETRY_SLEEP_SECONDS="${retry_sleep_seconds}" \
RETRY_SCHEDULE_SECONDS="${retry_schedule_seconds}" \
REQUIRE_SEARCH=0 \
REQUIRE_REGISTRATION="${require_registration}" \
REQUIRE_FLATCONTAINER=1 \
bash tools/ci/verify_nuget_release.sh
