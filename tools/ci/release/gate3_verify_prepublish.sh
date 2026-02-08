#!/usr/bin/env bash
set -euo pipefail

expected_version="${1:?expected version required}"
nupkg_path="${2:?nupkg path required}"
EXPECTED_VERSION="${expected_version}" NUPKG_PATH="${nupkg_path}" VERIFY_ONLINE=0 bash tools/ci/verify_nuget_release.sh
