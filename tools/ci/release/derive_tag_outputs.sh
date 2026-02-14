#!/usr/bin/env bash
set -euo pipefail

TAG="${RELEASE_TAG:-${GITHUB_REF_NAME:-}}"
OUTPUT_FILE="${GITHUB_OUTPUT:?GITHUB_OUTPUT is required}"
REGEX='^v([0-9]+)\.([0-9]+)\.([0-9]+)(-([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?$'

[[ -n "${TAG}" ]] || { echo "Release tag is required (RELEASE_TAG or GITHUB_REF_NAME)." >&2; exit 1; }
[[ "${TAG}" =~ ${REGEX} ]] || { echo "Invalid tag format '${TAG}'. Expected vMAJOR.MINOR.PATCH[-prerelease]." >&2; exit 1; }

MAJOR="${BASH_REMATCH[1]}"
MINOR="${BASH_REMATCH[2]}"
PATCH="${BASH_REMATCH[3]}"
VERSION="${TAG#v}"
ASSEMBLY_VERSION="${MAJOR}.${MINOR}.0.0"
FILE_VERSION="${MAJOR}.${MINOR}.${PATCH}.0"

{
  echo "version=${VERSION}"
  echo "tag=${TAG}"
  echo "assembly_version=${ASSEMBLY_VERSION}"
  echo "file_version=${FILE_VERSION}"
} >> "${OUTPUT_FILE}"
