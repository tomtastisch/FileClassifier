#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
LC_ALL=C

ROOT_DIR="${ROOT_DIR:-$(pwd)}"
REPO_SLUG="${REPO_SLUG:-tomtastisch/FileClassifier}"
PACKAGE_ID="${PACKAGE_ID:-Tomtastisch.FileClassifier}"
REQUIRE_REMOTE="${REQUIRE_REMOTE:-0}"
OUT_DIR="${OUT_DIR:-artifacts/ci/version-convergence}"

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

info() {
  echo "INFO: $*"
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "Missing command: $1"
}

retry_with_backoff() {
  local name="$1"
  shift
  local max_retries="${REMOTE_RETRY_COUNT:-3}"
  local sleep_seconds="${REMOTE_RETRY_INITIAL_DELAY_SECONDS:-2}"
  local attempt=1

  [[ "${max_retries}" =~ ^[0-9]+$ ]] || fail "REMOTE_RETRY_COUNT must be numeric"
  [[ "${sleep_seconds}" =~ ^[0-9]+$ ]] || fail "REMOTE_RETRY_INITIAL_DELAY_SECONDS must be numeric"

  while true; do
    if "$@"; then
      return 0
    fi
    if (( attempt > max_retries )); then
      fail "${name} failed after $((max_retries + 1)) attempts."
    fi
    info "${name} attempt ${attempt}/$((max_retries + 1)) failed; retrying in ${sleep_seconds}s."
    sleep "${sleep_seconds}"
    sleep_seconds=$((sleep_seconds * 2))
    attempt=$((attempt + 1))
  done
}

read_repo_version() {
  sed -n 's/.*<RepoVersion>\([^<]*\)<\/RepoVersion>.*/\1/p' "${ROOT_DIR}/Directory.Build.props" | head -n1
}

read_vbproj_field() {
  local field="$1"
  sed -n "s/.*<${field}>\\([^<]*\\)<\\/${field}>.*/\\1/p" "${ROOT_DIR}/src/FileTypeDetection/FileTypeDetectionLib.vbproj" | head -n1
}

read_docs_latest_version() {
  local line
  line="$(awk '/^\| `?[0-9]+\.[0-9]+\.[0-9]+`? \|/ { print; exit }' "${ROOT_DIR}/docs/versioning/002_HISTORY_VERSIONS.MD" || true)"
  [[ -n "${line}" ]] || fail "Could not read latest version row from docs/versioning/002_HISTORY_VERSIONS.MD"
  printf '%s\n' "${line}" | sed -E 's/^\| `?([0-9]+\.[0-9]+\.[0-9]+)`? \|.*$/\1/'
}

normalize_tag() {
  printf '%s' "$1" | sed -E 's/^v//'
}

read_nuget_latest_version() {
  local package_id_lc="$1"
  curl -fsSL "https://api.nuget.org/v3-flatcontainer/${package_id_lc}/index.json" | jq -r '.versions[-1]'
}

main() {
  require_cmd sed
  require_cmd awk
  require_cmd jq

  mkdir -p "${ROOT_DIR}/${OUT_DIR}"

  local repo_version vbproj_version vbproj_pkg_version docs_version
  repo_version="$(read_repo_version)"
  vbproj_version="$(read_vbproj_field Version)"
  vbproj_pkg_version="$(read_vbproj_field PackageVersion)"
  docs_version="$(read_docs_latest_version)"

  [[ -n "${repo_version}" ]] || fail "RepoVersion missing in Directory.Build.props"
  [[ -n "${vbproj_version}" ]] || fail "Version missing in FileTypeDetectionLib.vbproj"
  [[ -n "${vbproj_pkg_version}" ]] || fail "PackageVersion missing in FileTypeDetectionLib.vbproj"
  [[ -n "${docs_version}" ]] || fail "Latest docs version missing"

  [[ "${vbproj_version}" == "${repo_version}" ]] || fail "vbproj Version (${vbproj_version}) != RepoVersion (${repo_version})"
  [[ "${vbproj_pkg_version}" == "${repo_version}" ]] || fail "vbproj PackageVersion (${vbproj_pkg_version}) != RepoVersion (${repo_version})"
  [[ "${docs_version}" == "${repo_version}" ]] || fail "docs latest version (${docs_version}) != RepoVersion (${repo_version})"

  local release_tag="" release_version="" nuget_latest=""
  if [[ "${REQUIRE_REMOTE}" == "1" ]]; then
    require_cmd curl
    require_cmd gh
    export GH_TOKEN="${GH_TOKEN:-${GITHUB_TOKEN:-}}"
    [[ -n "${GH_TOKEN}" ]] || fail "REQUIRE_REMOTE=1 needs GH_TOKEN/GITHUB_TOKEN"

    release_tag="$(retry_with_backoff "github_release_lookup" gh api "repos/${REPO_SLUG}/releases/latest" --jq '.tag_name')"
    release_version="$(normalize_tag "${release_tag}")"
    [[ "${release_version}" == "${repo_version}" ]] || fail "GitHub latest release (${release_tag}) != RepoVersion (${repo_version})"

    local package_id_lc
    package_id_lc="$(printf '%s' "${PACKAGE_ID}" | tr '[:upper:]' '[:lower:]')"
    nuget_latest="$(retry_with_backoff "nuget_version_lookup" read_nuget_latest_version "${package_id_lc}")"
    [[ -n "${nuget_latest}" && "${nuget_latest}" != "null" ]] || fail "Could not read latest NuGet version for ${PACKAGE_ID}"
    [[ "${nuget_latest}" == "${repo_version}" ]] || fail "NuGet latest (${nuget_latest}) != RepoVersion (${repo_version})"
  fi

  jq -n \
    --arg repo_version "${repo_version}" \
    --arg vbproj_version "${vbproj_version}" \
    --arg vbproj_package_version "${vbproj_pkg_version}" \
    --arg docs_version "${docs_version}" \
    --arg release_tag "${release_tag}" \
    --arg release_version "${release_version}" \
    --arg nuget_latest "${nuget_latest}" \
    --arg require_remote "${REQUIRE_REMOTE}" \
    '{
      check_id: "version-convergence",
      status: "pass",
      require_remote: ($require_remote == "1"),
      repo_version: $repo_version,
      vbproj_version: $vbproj_version,
      vbproj_package_version: $vbproj_package_version,
      docs_latest_version: $docs_version,
      github_latest_release_tag: $release_tag,
      github_latest_release_version: $release_version,
      nuget_latest_version: $nuget_latest
    }' > "${ROOT_DIR}/${OUT_DIR}/summary.json"

  info "Version convergence passed (repo=${repo_version}, remote_check=${REQUIRE_REMOTE})."
}

main "$@"
