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
VERIFY_ONLINE="${VERIFY_ONLINE:-1}"
REQUIRE_SEARCH="${REQUIRE_SEARCH:-1}"
REQUIRE_REGISTRATION="${REQUIRE_REGISTRATION:-1}"
REQUIRE_FLATCONTAINER="${REQUIRE_FLATCONTAINER:-1}"

SEARCH_OK="skipped"
REGISTRATION_OK="skipped"
FLATCONTAINER_OK="skipped"
REGISTRATION_URL=""
SEARCH_URL=""
FLAT_URL=""

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

retry_network() {
  local name="$1"
  shift
  local attempt=0
  local max_attempts=$((RETRY_COUNT + 1))
  while true; do
    if "$@"; then
      if [[ "${attempt}" -gt 0 ]]; then
        info "Network check '${name}' succeeded on attempt $((attempt + 1))/${max_attempts}."
      fi
      return 0
    fi
    if [[ "${attempt}" -ge "${RETRY_COUNT}" ]]; then
      fail "Network check '${name}' failed after $((RETRY_COUNT + 1)) attempts."
    fi
    attempt=$((attempt + 1))
    info "Network check '${name}' attempt ${attempt}/${max_attempts} failed; retrying in ${RETRY_SLEEP_SECONDS}s."
    sleep "${RETRY_SLEEP_SECONDS}"
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
  python3 - "$filename" <<'PY'
import re
import sys

name = sys.argv[1]
m = re.match(r'^(?P<id>.+)\.(?P<ver>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)\.nupkg$', name)
if not m:
    print()
    print()
    sys.exit(0)
print(m.group("id"))
print(m.group("ver"))
PY
}

derive_from_nuspec() {
  local nuspec_xml
  nuspec_xml="$(unzip -p "${NUPKG_PATH}" '*.nuspec' 2>/dev/null)" || fail "Unable to read .nuspec from ${NUPKG_PATH}"

  local parsed
  parsed="$(NUSPEC_XML="${nuspec_xml}" python3 - <<'PY'
import re
import sys
import xml.etree.ElementTree as ET
import os

text = os.environ.get("NUSPEC_XML", "")
try:
    root = ET.fromstring(text)
except ET.ParseError:
    def by_regex(tag):
        m = re.search(rf'<{tag}>\s*([^<]+?)\s*</{tag}>', text, flags=re.IGNORECASE)
        return m.group(1).strip() if m else ""
    print(by_regex("id"))
    print(by_regex("version"))
    sys.exit(0)

def find_first(node, tag_name):
    for elem in node.iter():
        local = elem.tag.rsplit('}', 1)[-1]
        if local == tag_name and elem.text:
            return elem.text.strip()
    return ""

print(find_first(root, "id"))
print(find_first(root, "version"))
PY
)"

  printf '%s\n' "${parsed}"
}

query_search() {
  SEARCH_URL="https://azuresearch-usnc.nuget.org/query?q=packageid:${PKG_ID}&take=5"
  local response
  response="$(curl -fsS --max-time "${TIMEOUT_SECONDS}" "${SEARCH_URL}")" || return 1

  local out
  out="$(SEARCH_RESPONSE="${response}" python3 - "$PKG_ID" "$PKG_VER" <<'PY'
import json
import sys
import os

pkg = sys.argv[1].lower()
ver = sys.argv[2]
data = json.loads(os.environ.get("SEARCH_RESPONSE", "{}"))
registration = ""
has_id = False
has_ver = False

for item in data.get("data", []):
    item_id = str(item.get("id", ""))
    if item_id.lower() != pkg:
        continue
    has_id = True
    for v in item.get("versions", []):
        if isinstance(v, dict) and str(v.get("version", "")) == ver:
            has_ver = True
            break
    if item.get("registration"):
        registration = str(item["registration"])

if not has_id:
    print("missing_id", file=sys.stderr)
    sys.exit(2)
if not has_ver:
    print("missing_version", file=sys.stderr)
    sys.exit(3)
if not registration:
    print("missing_registration", file=sys.stderr)
    sys.exit(4)
print(registration)
PY
)" || return 1

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
  response="$(curl -fsS --max-time "${TIMEOUT_SECONDS}" "${REGISTRATION_URL}")" || return 1

  REGISTRATION_RESPONSE="${response}" python3 - "$PKG_VER" <<'PY' >/dev/null || return 1
import json
import sys
import os

target = sys.argv[1].lower()
obj = json.loads(os.environ.get("REGISTRATION_RESPONSE", "{}"))
found = False

def walk(node):
    global found
    if found:
        return
    if isinstance(node, dict):
        for k, v in node.items():
            if k.lower() == "version" and isinstance(v, str) and v.lower() == target:
                found = True
                return
            walk(v)
    elif isinstance(node, list):
        for item in node:
            walk(item)

walk(obj)
if not found:
    sys.exit(2)
PY

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

emit_summary_json() {
  python3 - <<'PY'
import json
import os

print(json.dumps({
    "id": os.environ.get("PKG_ID", ""),
    "version": os.environ.get("PKG_VER", ""),
    "expected": os.environ.get("EXPECTED_VERSION", ""),
    "verify_online": os.environ.get("VERIFY_ONLINE", ""),
    "require_search": os.environ.get("REQUIRE_SEARCH", ""),
    "require_registration": os.environ.get("REQUIRE_REGISTRATION", ""),
    "require_flatcontainer": os.environ.get("REQUIRE_FLATCONTAINER", ""),
    "registration": os.environ.get("REGISTRATION_URL", ""),
    "search": os.environ.get("SEARCH_OK", "skipped"),
    "registration_check": os.environ.get("REGISTRATION_OK", "skipped"),
    "flatcontainer": os.environ.get("FLATCONTAINER_OK", "skipped")
}, separators=(",", ":")))
PY
}

main() {
  require_cmd curl
  require_cmd unzip
  require_cmd python3
  require_bool_flag "REQUIRE_SEARCH" "${REQUIRE_SEARCH}"
  require_bool_flag "REQUIRE_REGISTRATION" "${REQUIRE_REGISTRATION}"
  require_bool_flag "REQUIRE_FLATCONTAINER" "${REQUIRE_FLATCONTAINER}"

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
  else
    info "Online checks skipped (VERIFY_ONLINE=${VERIFY_ONLINE})."
  fi

  export PKG_ID PKG_VER EXPECTED_VERSION VERIFY_ONLINE REQUIRE_SEARCH REQUIRE_REGISTRATION REQUIRE_FLATCONTAINER REGISTRATION_URL SEARCH_OK REGISTRATION_OK FLATCONTAINER_OK
  emit_summary_json
  echo "OK: verify_nuget_release completed."
}

main "$@"
