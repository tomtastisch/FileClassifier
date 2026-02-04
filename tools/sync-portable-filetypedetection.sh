#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="${ROOT_DIR}/src/FileTypeDetection"
OUT_DIR="${ROOT_DIR}/portable/FileTypeDetection"
source "${ROOT_DIR}/tools/filetypedetection-sync-lib.sh"

if [[ ! -d "${SRC_DIR}" ]]; then
  echo "Source directory not found: ${SRC_DIR}" >&2
  exit 1
fi

TMP_SRC_MANIFEST="$(mktemp /tmp/filetypedetection-src-manifest.XXXXXX)"
TMP_OUT_MANIFEST="$(mktemp /tmp/filetypedetection-out-manifest.XXXXXX)"
trap 'rm -f "${TMP_SRC_MANIFEST}" "${TMP_OUT_MANIFEST}"' EXIT

# Source of truth: all VB and Markdown files (excluding build artifacts and project manifests).
build_source_manifest "${SRC_DIR}" "${TMP_SRC_MANIFEST}"

rm -rf "${OUT_DIR}"
mkdir -p "${OUT_DIR}"

while IFS=$'\t' read -r rel _hash; do
  mkdir -p "${OUT_DIR}/$(dirname "${rel}")"
  cp "${SRC_DIR}/${rel}" "${OUT_DIR}/${rel}"
done < "${TMP_SRC_MANIFEST}"

# Keep top-level portable README as a strict mirror of source README.
cp "${ROOT_DIR}/src/README.md" "${ROOT_DIR}/portable/README.md"

build_portable_manifest "${OUT_DIR}" "${TMP_OUT_MANIFEST}"

if ! diff -u "${TMP_SRC_MANIFEST}" "${TMP_OUT_MANIFEST}" >/dev/null; then
  echo "Portable sync failed: source and portable manifests differ." >&2
  diff -u "${TMP_SRC_MANIFEST}" "${TMP_OUT_MANIFEST}" || true
  exit 1
fi

echo "Portable sync complete (1:1 for *.vb + *.md): ${OUT_DIR}"
