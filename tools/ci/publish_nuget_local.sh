#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
LC_ALL=C

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
ARTIFACT_DIR="${REPO_ROOT}/artifacts/nuget"
PACKAGE_PROJECT="${REPO_ROOT}/src/FileTypeDetection/FileTypeDetectionLib.vbproj"
NUGET_SOURCE_URL="https://api.nuget.org/v3/index.json"
EXPECTED_VERSION="${EXPECTED_VERSION:-}"
REPOSITORY_TAG="${REPOSITORY_TAG:-}"
NUGET_API_KEY="${NUGET_API_KEY:-}"

fail() {
  echo "FAIL: $*" >&2
  exit 1
}

info() {
  echo "INFO: $*"
}

if [[ -z "${EXPECTED_VERSION}" ]]; then
  mapfile -t tags < <(git -C "${REPO_ROOT}" tag --points-at HEAD | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$' | LC_ALL=C sort)
  if [[ "${#tags[@]}" -eq 1 ]]; then
    EXPECTED_VERSION="${tags[0]#v}"
    REPOSITORY_TAG="${tags[0]}"
  else
    fail "EXPECTED_VERSION missing and git tag on HEAD not deterministically resolvable."
  fi
fi

if [[ -z "${REPOSITORY_TAG}" ]]; then
  REPOSITORY_TAG="v${EXPECTED_VERSION}"
fi

if [[ -z "${NUGET_API_KEY}" ]]; then
  NUGET_API_KEY="$(
    python3 "${REPO_ROOT}/tools/ci/bin/keychain_get_secret.py" --service "NUGET_API_KEY" || true
  )"
fi
[[ -n "${NUGET_API_KEY}" ]] || fail "No NuGet API key found (env NUGET_API_KEY or macOS keychain service 'NUGET_API_KEY')."

mkdir -p "${ARTIFACT_DIR}"

info "Packing with EXPECTED_VERSION=${EXPECTED_VERSION}"
dotnet pack "${PACKAGE_PROJECT}" \
  -c Release \
  -o "${ARTIFACT_DIR}" \
  -p:PackageVersion="${EXPECTED_VERSION}" \
  -p:Version="${EXPECTED_VERSION}" \
  -p:ContinuousIntegrationBuild=true \
  -p:RepositoryTag="${REPOSITORY_TAG}" \
  -v minimal

info "Running local pre-publish verification"
EXPECTED_VERSION="${EXPECTED_VERSION}" \
NUPKG_DIR="${ARTIFACT_DIR}" \
VERIFY_ONLINE=0 \
bash "${REPO_ROOT}/tools/ci/verify_nuget_release.sh"

mapfile -t nupkgs < <(find "${ARTIFACT_DIR}" -maxdepth 1 -type f -name "*.nupkg" ! -name "*.snupkg" | LC_ALL=C sort)
[[ "${#nupkgs[@]}" -gt 0 ]] || fail "No nupkg found for push."

NUPKG_PATH=""
if [[ "${#nupkgs[@]}" -eq 1 ]]; then
  NUPKG_PATH="${nupkgs[0]}"
else
  mapfile -t matches < <(printf '%s\n' "${nupkgs[@]}" | awk -v v="${EXPECTED_VERSION}" '
    {
      n=$0
      sub(/^.*\//, "", n)
      if (n ~ ("\\." v "\\.nupkg$")) {
        print $0
      }
    }')
  [[ "${#matches[@]}" -eq 1 ]] || fail "Unable to deterministically select nupkg for EXPECTED_VERSION=${EXPECTED_VERSION}"
  NUPKG_PATH="${matches[0]}"
fi

info "Publishing $(basename "${NUPKG_PATH}") to NuGet.org"
dotnet nuget push "${NUPKG_PATH}" \
  --api-key "${NUGET_API_KEY}" \
  --source "${NUGET_SOURCE_URL}" \
  --skip-duplicate

info "Running post-publish verification"
EXPECTED_VERSION="${EXPECTED_VERSION}" \
NUPKG_PATH="${NUPKG_PATH}" \
bash "${REPO_ROOT}/tools/ci/verify_nuget_release.sh"

echo "OK: publish_nuget_local completed."
