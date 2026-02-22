#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"

PACKAGE_PROJECT="${ROOT_DIR}/src/FileTypeDetection/FileTypeDetectionLib.vbproj"
TEST_PROJECT="${ROOT_DIR}/tests/PackageBacked.Tests/PackageBacked.Tests.csproj"
LOCAL_FEED_DIR="${ROOT_DIR}/artifacts/ci/pack/nuget"

find_pack_nupkg() {
  local pack_dir="$1"
  python3 - "$pack_dir" <<'PY'
import glob
import os
import sys

pack_dir = sys.argv[1]
candidates = [
    path for path in glob.glob(os.path.join(pack_dir, "*.nupkg"))
    if not path.endswith(".snupkg")
]
if not candidates:
    sys.exit(1)

candidates.sort(key=lambda path: os.path.getmtime(path), reverse=True)
print(candidates[0])
PY
}

read_nupkg_version() {
  local nupkg_path="$1"
  local nuspec_xml

  nuspec_xml="$(unzip -p "${nupkg_path}" '*.nuspec' 2>/dev/null)"
  printf '%s\n' "${nuspec_xml}" | tr -d '\r' | sed -n 's/.*<version>\([^<]*\)<\/version>.*/\1/p' | head -n1
}

mkdir -p "${LOCAL_FEED_DIR}"

dotnet restore --locked-mode "${PACKAGE_PROJECT}" -v minimal
dotnet build "${PACKAGE_PROJECT}" -c Release --no-restore -warnaserror -v minimal
dotnet pack "${PACKAGE_PROJECT}" -c Release --no-build -o "${LOCAL_FEED_DIR}" -v minimal

NUPKG_PATH="$(find_pack_nupkg "${LOCAL_FEED_DIR}")"
PACKAGE_VERSION="$(read_nupkg_version "${NUPKG_PATH}")"

if [[ -z "${PACKAGE_VERSION}" ]]; then
  echo "ERROR: could not read package version from ${NUPKG_PATH}" >&2
  exit 1
fi

dotnet restore "${TEST_PROJECT}" \
  --source "${LOCAL_FEED_DIR}" \
  --source "https://api.nuget.org/v3/index.json" \
  -p:PackageBackedVersion="${PACKAGE_VERSION}" \
  -p:RestoreLockedMode=false \
  --force-evaluate \
  -v minimal

dotnet test "${TEST_PROJECT}" -c Release --no-restore -p:PackageBackedVersion="${PACKAGE_VERSION}" -v minimal

echo "INFO: package-backed tests passed against package version ${PACKAGE_VERSION}"
