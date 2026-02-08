#!/usr/bin/env bash
set -euo pipefail

MODE="${MODE:-ci}"
TAG="${TAG:-${GITHUB_REF_NAME:-}}"
NUPKG_PATH="${NUPKG_PATH:-}"

readonly TAG_REGEX='^v([0-9]+)\.([0-9]+)\.([0-9]+)(-([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?$'

print_header() {
  echo "version-policy: mode=${MODE}"
}

validate_tag() {
  local tag="$1"
  if [[ -z "${tag}" ]]; then
    echo "version-policy: TAG/GITHUB_REF_NAME missing" >&2
    return 1
  fi

  if [[ ! "${tag}" =~ ${TAG_REGEX} ]]; then
    echo "version-policy: invalid release tag '${tag}' (expected vMAJOR.MINOR.PATCH[-prerelease])" >&2
    return 1
  fi

  echo "${tag#v}"
}

collect_version_policy_violations() {
  local -a files=()
  local -a violations=()
  local -a patterns=(
    '<Version>'
    '<PackageVersion>'
    '<VersionPrefix>'
    '<VersionSuffix>'
    '<InformationalVersion>'
    '<AssemblyVersion>'
    '<FileVersion>'
  )

  while IFS= read -r file; do
    files+=("${file}")
  done < <(rg --files -g 'Directory.Build.props' -g 'Directory.Build.targets' -g '*.csproj' -g '*.vbproj')

  for file in "${files[@]}"; do
    for pattern in "${patterns[@]}"; do
      if rg -n --fixed-strings "${pattern}" "${file}" >/dev/null 2>&1; then
        while IFS= read -r line; do
          violations+=("${file}:${line}")
        done < <(rg -n --fixed-strings "${pattern}" "${file}")
      fi
    done
  done

  printf "%s\n" "${violations[@]}"
}

run_ci_mode() {
  local violations
  violations="$(collect_version_policy_violations)"
  if [[ -n "${violations}" ]]; then
    echo "version-policy: static version fields are forbidden (tag is SSOT)." >&2
    echo "${violations}" >&2
    return 1
  fi

  echo "version-policy: no static package/assembly version fields detected."
}

read_nupkg_version() {
  local nupkg="$1"
  if [[ ! -f "${nupkg}" ]]; then
    echo "version-policy: nupkg not found at '${nupkg}'" >&2
    return 1
  fi

  local nuspec_xml
  if ! nuspec_xml="$(unzip -p "${nupkg}" '*.nuspec' 2>/dev/null)"; then
    echo "version-policy: unable to read nuspec from '${nupkg}'" >&2
    return 1
  fi

  local pkg_version
  pkg_version="$(printf '%s\n' "${nuspec_xml}" | tr -d '\r' | sed -n 's/.*<version>\([^<]*\)<\/version>.*/\1/p' | head -n1)"
  if [[ -z "${pkg_version}" ]]; then
    echo "version-policy: package version not found in nuspec (${nupkg})" >&2
    return 1
  fi

  echo "${pkg_version}"
}

run_release_mode() {
  local expected_version
  expected_version="$(validate_tag "${TAG}")"

  if [[ -z "${NUPKG_PATH}" ]]; then
    echo "version-policy: NUPKG_PATH missing in release mode" >&2
    return 1
  fi

  local actual_version
  actual_version="$(read_nupkg_version "${NUPKG_PATH}")"

  echo "version-policy: tag_version=${expected_version}"
  echo "version-policy: package_version=${actual_version}"

  if [[ "${actual_version}" != "${expected_version}" ]]; then
    echo "version-policy: tag/package mismatch (expected ${expected_version}, got ${actual_version})" >&2
    return 1
  fi

  echo "version-policy: tag version matches nupkg metadata (1:1)."
}

main() {
  print_header
  case "${MODE}" in
    ci) run_ci_mode ;;
    release) run_release_mode ;;
    *)
      echo "version-policy: unsupported mode '${MODE}' (expected ci|release)" >&2
      return 2
      ;;
  esac
}

main
