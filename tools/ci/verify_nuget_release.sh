#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
LC_ALL=C

NUPKG_PATH="${NUPKG_PATH:-}"
NUPKG_DIR="${NUPKG_DIR:-artifacts/nuget}"
PKG_ID="${PKG_ID:-}"
PKG_VER="${PKG_VER:-}"
EXPECTED_VERSION="${EXPECTED_VERSION:-}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-30}"
RETRY_COUNT="${RETRY_COUNT:-6}"
RETRY_SLEEP_SECONDS="${RETRY_SLEEP_SECONDS:-10}"
RETRY_SCHEDULE_SECONDS="${RETRY_SCHEDULE_SECONDS:-}"
VERIFY_ONLINE="${VERIFY_ONLINE:-1}"
REQUIRE_SEARCH="${REQUIRE_SEARCH:-1}"
REQUIRE_REGISTRATION="${REQUIRE_REGISTRATION:-1}"
REQUIRE_FLATCONTAINER="${REQUIRE_FLATCONTAINER:-1}"
REQUIRE_V2_DOWNLOAD="${REQUIRE_V2_DOWNLOAD:-0}"

SEARCH_OK="skipped"
REGISTRATION_OK="skipped"
FLATCONTAINER_OK="skipped"
V2_DOWNLOAD_OK="skipped"
REGISTRATION_URL=""
SEARCH_URL=""
FLAT_URL=""
V2_URL=""

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

info() {
  echo "INFO: $*"
}

require_cmd() {
  local cmd="$1"
  command -v "$cmd" >/dev/null 2>&1 || fail "Required command missing: ${cmd}"
}

require_bool_flag() {
  local name="$1"
  local value="$2"
  if [[ "${value}" != "0" && "${value}" != "1" ]]; then
    fail "${name} must be 0 or 1 (actual='${value}')"
  fi
}

require_nonnegative_integer() {
  local name="$1"
  local value="$2"
  if [[ ! "${value}" =~ ^[0-9]+$ ]]; then
    fail "${name} must be a non-negative integer (actual='${value}')"
  fi
}

retry_network() {
  local name="$1"
  shift
  local attempt=0
  local max_attempts=$((RETRY_COUNT + 1))
  while true; do
    local current_attempt=$((attempt + 1))
    if "$@"; then
      if [[ "${attempt}" -gt 0 ]]; then
        info "Network check '${name}' succeeded on attempt ${current_attempt}/${max_attempts}."
      fi
      return 0
    fi
    if [[ "${attempt}" -ge "${RETRY_COUNT}" ]]; then
      fail "Network check '${name}' failed after $((RETRY_COUNT + 1)) attempts."
    fi
    local sleep_seconds="${RETRY_SLEEP_SECONDS}"
    if [[ -n "${RETRY_SCHEDULE_SECONDS}" ]]; then
      local schedule_value
      schedule_value="$(awk -F',' -v idx="${current_attempt}" '{ if (idx <= NF) print $idx; else print $NF }' <<<"${RETRY_SCHEDULE_SECONDS}")"
      if [[ -z "${schedule_value}" || ! "${schedule_value}" =~ ^[0-9]+$ ]]; then
        fail "RETRY_SCHEDULE_SECONDS must contain only comma-separated non-negative integers."
      fi
      sleep_seconds="${schedule_value}"
    fi
    info "Network check '${name}' attempt ${current_attempt}/${max_attempts} failed; retrying in ${sleep_seconds}s."
    attempt=$((attempt + 1))
    sleep "${sleep_seconds}"
  done
}

resolve_nupkg_path() {
  if [[ -n "${NUPKG_PATH}" ]]; then
    [[ -f "${NUPKG_PATH}" ]] || fail "NUPKG_PATH not found: ${NUPKG_PATH}"
    return 0
  fi

  [[ -d "${NUPKG_DIR}" ]] || fail "NUPKG_DIR not found: ${NUPKG_DIR}"

  mapfile -t nupkgs < <(find "${NUPKG_DIR}" -maxdepth 1 -type f -name '*.nupkg' ! -name '*.snupkg' | LC_ALL=C sort)
  [[ "${#nupkgs[@]}" -gt 0 ]] || fail "No .nupkg found in ${NUPKG_DIR}"

  if [[ "${#nupkgs[@]}" -eq 1 ]]; then
    NUPKG_PATH="${nupkgs[0]}"
    return 0
  fi

  if [[ -n "${EXPECTED_VERSION}" ]]; then
    mapfile -t matches < <(printf '%s\n' "${nupkgs[@]}" | awk -v v="${EXPECTED_VERSION}" '
      {
        n=$0
        sub(/^.*\//, "", n)
        if (n ~ ("\\." v "\\.nupkg$")) {
          print $0
        }
      }')
    if [[ "${#matches[@]}" -eq 1 ]]; then
      NUPKG_PATH="${matches[0]}"
      return 0
    fi
    fail "Multiple .nupkg files found in ${NUPKG_DIR}; deterministic selection by EXPECTED_VERSION=${EXPECTED_VERSION} failed."
  fi

  fail "Multiple .nupkg files found in ${NUPKG_DIR}; set NUPKG_PATH or EXPECTED_VERSION."
}

derive_from_filename() {
  local filename
  filename="$(basename "${NUPKG_PATH}")"
  python3 tools/ci/bin/verify_nuget_release_helpers.py derive-filename --filename "$filename"
}

derive_from_nuspec() {
  local nuspec_xml
  nuspec_xml="$(unzip -p "${NUPKG_PATH}" '*.nuspec' 2>/dev/null)" || fail "Unable to read .nuspec from ${NUPKG_PATH}"

  local parsed
  parsed="$(python3 tools/ci/bin/verify_nuget_release_helpers.py derive-nuspec --nuspec-xml "${nuspec_xml}")"

  printf '%s\n' "${parsed}"
}

query_search() {
  # Always query with prerelease + SemVer2 enabled so RC versions are visible
  # in the search index during convergence checks.
  SEARCH_URL="https://azuresearch-usnc.nuget.org/query?q=packageid:${PKG_ID}&take=5&prerelease=true&semVerLevel=2.0.0"
  local response
  response="$(curl -fsS --compressed --max-time "${TIMEOUT_SECONDS}" "${SEARCH_URL}")" || return 1

  local out
  out="$(python3 tools/ci/bin/verify_nuget_release_helpers.py query-search --response-json "${response}" --pkg-id "$PKG_ID" --pkg-ver "$PKG_VER")" || return 1

  REGISTRATION_URL="${out}"
  SEARCH_OK="ok"
  return 0
}

query_registration() {
  if [[ -z "${REGISTRATION_URL}" ]]; then
    local pkg_id_lc
    pkg_id_lc="$(printf '%s' "${PKG_ID}" | tr '[:upper:]' '[:lower:]')"
    REGISTRATION_URL="https://api.nuget.org/v3/registration5-semver1/${pkg_id_lc}/index.json"
  fi

  local response
  response="$(curl -fsS --compressed --max-time "${TIMEOUT_SECONDS}" "${REGISTRATION_URL}")" || return 1

  python3 tools/ci/bin/verify_nuget_release_helpers.py registration-contains --response-json "${response}" --pkg-ver "$PKG_VER" >/dev/null || return 1

  REGISTRATION_OK="ok"
  return 0
}

query_flatcontainer() {
  local pkg_id_lc
  pkg_id_lc="$(printf '%s' "${PKG_ID}" | tr '[:upper:]' '[:lower:]')"
  FLAT_URL="https://api.nuget.org/v3-flatcontainer/${pkg_id_lc}/${PKG_VER}/${pkg_id_lc}.${PKG_VER}.nupkg"

  local status
  status="$(curl -sS -o /dev/null -w '%{http_code}' --head --max-time "${TIMEOUT_SECONDS}" "${FLAT_URL}" || true)"
  if [[ "${status}" == "200" ]]; then
    FLATCONTAINER_OK="ok"
    return 0
  fi

  status="$(curl -sS -o /dev/null -w '%{http_code}' --range 0-0 --max-time "${TIMEOUT_SECONDS}" "${FLAT_URL}" || true)"
  if [[ "${status}" == "200" || "${status}" == "206" ]]; then
    FLATCONTAINER_OK="ok"
    return 0
  fi
  return 1
}

query_v2_download() {
  local pkg_id_lc
  pkg_id_lc="$(printf '%s' "${PKG_ID}" | tr '[:upper:]' '[:lower:]')"
  V2_URL="https://www.nuget.org/api/v2/package/${pkg_id_lc}/${PKG_VER}"
  local status
  status="$(curl -sS -o /dev/null -w '%{http_code}' -L --max-time "${TIMEOUT_SECONDS}" "${V2_URL}" || true)"
  if [[ "${status}" == "200" ]]; then
    V2_DOWNLOAD_OK="ok"
    return 0
  fi
  return 1
}

emit_summary_json() {
  python3 tools/ci/bin/verify_nuget_release_helpers.py emit-summary
}

main() {
  require_cmd curl
  require_cmd python3
  require_nonnegative_integer "RETRY_COUNT" "${RETRY_COUNT}"
  require_nonnegative_integer "RETRY_SLEEP_SECONDS" "${RETRY_SLEEP_SECONDS}"
  if [[ -n "${RETRY_SCHEDULE_SECONDS}" ]]; then
    if [[ ! "${RETRY_SCHEDULE_SECONDS}" =~ ^[0-9]+(,[0-9]+)*$ ]]; then
      fail "RETRY_SCHEDULE_SECONDS must be comma-separated non-negative integers (actual='${RETRY_SCHEDULE_SECONDS}')"
    fi
  fi
  require_bool_flag "REQUIRE_SEARCH" "${REQUIRE_SEARCH}"
  require_bool_flag "REQUIRE_REGISTRATION" "${REQUIRE_REGISTRATION}"
  require_bool_flag "REQUIRE_FLATCONTAINER" "${REQUIRE_FLATCONTAINER}"
  require_bool_flag "REQUIRE_V2_DOWNLOAD" "${REQUIRE_V2_DOWNLOAD}"

  if [[ -n "${PKG_ID}" || -n "${PKG_VER}" ]]; then
    if [[ -z "${PKG_ID}" || -z "${PKG_VER}" ]]; then
      fail "When using explicit package metadata, both PKG_ID and PKG_VER must be set."
    fi
    info "Using explicit package metadata: ${PKG_ID} ${PKG_VER}"
  else
    require_cmd unzip
    resolve_nupkg_path
    info "Using package file: ${NUPKG_PATH}"

    local fn_id fn_ver
    fn_id=""
    fn_ver=""
    mapfile -t parsed_from_filename < <(derive_from_filename)
    if [[ "${#parsed_from_filename[@]}" -ge 2 ]]; then
      fn_id="${parsed_from_filename[0]}"
      fn_ver="${parsed_from_filename[1]}"
    fi

    if [[ -z "${PKG_ID}" && -n "${fn_id}" ]]; then
      PKG_ID="${fn_id}"
    fi
    if [[ -z "${PKG_VER}" && -n "${fn_ver}" ]]; then
      PKG_VER="${fn_ver}"
    fi

    if [[ -z "${PKG_ID}" || -z "${PKG_VER}" ]]; then
      local ns_id ns_ver
      ns_id=""
      ns_ver=""
      mapfile -t parsed_from_nuspec < <(derive_from_nuspec)
      if [[ "${#parsed_from_nuspec[@]}" -ge 2 ]]; then
        ns_id="${parsed_from_nuspec[0]}"
        ns_ver="${parsed_from_nuspec[1]}"
      fi
      if [[ -z "${PKG_ID}" ]]; then
        PKG_ID="${ns_id}"
      fi
      if [[ -z "${PKG_VER}" ]]; then
        PKG_VER="${ns_ver}"
      fi
    fi
  fi

  [[ -n "${PKG_ID}" ]] || fail "Unable to resolve PKG_ID from filename or .nuspec."
  [[ -n "${PKG_VER}" ]] || fail "Unable to resolve PKG_VER from filename or .nuspec."
  info "Resolved package id/version: ${PKG_ID} ${PKG_VER}"

  if [[ -n "${EXPECTED_VERSION}" && "${PKG_VER}" != "${EXPECTED_VERSION}" ]]; then
    fail "ExpectedVersion mismatch: expected='${EXPECTED_VERSION}' actual='${PKG_VER}'"
  fi
  if [[ -n "${EXPECTED_VERSION}" ]]; then
    info "Version gate passed: expected='${EXPECTED_VERSION}'"
  fi

  if [[ "${VERIFY_ONLINE}" == "1" ]]; then
    if [[ "${REQUIRE_SEARCH}" == "1" ]]; then
      retry_network "search" query_search
      info "Search check OK: ${SEARCH_URL}"
    else
      info "Search check skipped (REQUIRE_SEARCH=${REQUIRE_SEARCH})."
    fi

    if [[ "${REQUIRE_REGISTRATION}" == "1" ]]; then
      retry_network "registration" query_registration
      info "Registration check OK: ${REGISTRATION_URL}"
    else
      info "Registration check skipped (REQUIRE_REGISTRATION=${REQUIRE_REGISTRATION})."
    fi

    if [[ "${REQUIRE_FLATCONTAINER}" == "1" ]]; then
      retry_network "flatcontainer" query_flatcontainer
      info "Flatcontainer check OK: ${FLAT_URL}"
    else
      info "Flatcontainer check skipped (REQUIRE_FLATCONTAINER=${REQUIRE_FLATCONTAINER})."
    fi

    if [[ "${REQUIRE_V2_DOWNLOAD}" == "1" ]]; then
      retry_network "v2-download" query_v2_download
      info "V2 download check OK: ${V2_URL}"
    else
      info "V2 download check skipped (REQUIRE_V2_DOWNLOAD=${REQUIRE_V2_DOWNLOAD})."
    fi
  else
    info "Online checks skipped (VERIFY_ONLINE=${VERIFY_ONLINE})."
  fi

  export PKG_ID PKG_VER EXPECTED_VERSION VERIFY_ONLINE REQUIRE_SEARCH REQUIRE_REGISTRATION REQUIRE_FLATCONTAINER REQUIRE_V2_DOWNLOAD REGISTRATION_URL SEARCH_OK REGISTRATION_OK FLATCONTAINER_OK V2_DOWNLOAD_OK
  emit_summary_json
  echo "OK: verify_nuget_release completed."
}

main "$@"
