#!/usr/bin/env bash
set -euo pipefail

mode="${1:-release}"
tag="${2:?tag required}"
nupkg_path="${3:?nupkg path required}"
MODE="${mode}" TAG="${tag}" NUPKG_PATH="${nupkg_path}" bash tools/versioning/check-version-policy.sh
