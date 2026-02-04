#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_ROOT="${ROOT_DIR}/src/FileTypeDetection"
TEST_ROOT="${ROOT_DIR}/tests/FileTypeDetectionLib.Tests"
PORTABLE_ROOT="${ROOT_DIR}/portable/FileTypeDetection"
source "${ROOT_DIR}/tools/filetypedetection-sync-lib.sh"

# Non-destructive validator.
# This script never overwrites documentation files.

required_files=(
  "${ROOT_DIR}/README.md"
  "${ROOT_DIR}/src/README.md"
  "${SRC_ROOT}/INDEX.md"
  "${SRC_ROOT}/docs/API_REFERENCE.md"
  "${SRC_ROOT}/Abstractions/INDEX.md"
  "${SRC_ROOT}/Configuration/INDEX.md"
  "${SRC_ROOT}/Detection/INDEX.md"
  "${SRC_ROOT}/Infrastructure/INDEX.md"
  "${TEST_ROOT}/INDEX.md"
  "${TEST_ROOT}/Unit/INDEX.md"
  "${PORTABLE_ROOT}/INDEX.md"
  "${PORTABLE_ROOT}/docs/API_REFERENCE.md"
  "${ROOT_DIR}/portable/README.md"
)

missing=0
for f in "${required_files[@]}"; do
  if [[ ! -f "${f}" ]]; then
    echo "Missing required documentation file: ${f}" >&2
    missing=1
  fi
done

if [[ "${missing}" -ne 0 ]]; then
  exit 1
fi

TMP_SRC_MANIFEST="$(mktemp /tmp/filetypedetection-doc-src.XXXXXX)"
TMP_PORTABLE_MANIFEST="$(mktemp /tmp/filetypedetection-doc-portable.XXXXXX)"
trap 'rm -f "${TMP_SRC_MANIFEST}" "${TMP_PORTABLE_MANIFEST}"' EXIT

build_source_manifest "${SRC_ROOT}" "${TMP_SRC_MANIFEST}"
build_portable_manifest "${PORTABLE_ROOT}" "${TMP_PORTABLE_MANIFEST}"

if ! diff -u "${TMP_SRC_MANIFEST}" "${TMP_PORTABLE_MANIFEST}" >/dev/null; then
  echo "Mismatch detected between src/FileTypeDetection and portable/FileTypeDetection (*.vb + *.md)." >&2
  echo "Run: bash tools/sync-portable-filetypedetection.sh" >&2
  diff -u "${TMP_SRC_MANIFEST}" "${TMP_PORTABLE_MANIFEST}" || true
  exit 1
fi

if ! cmp -s "${ROOT_DIR}/src/README.md" "${ROOT_DIR}/portable/README.md"; then
  echo "Mismatch detected: portable/README.md is not 1:1 with src/README.md." >&2
  echo "Run: bash tools/sync-portable-filetypedetection.sh" >&2
  exit 1
fi

echo "Documentation conventions validated (non-destructive)."
echo "Portable mirror is 1:1 with source for *.vb + *.md, and src/README.md is mirrored."
