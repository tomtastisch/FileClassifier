#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
LC_ALL=C

ROOT_DIR="$(pwd)"
NAMING_SSOT=""
VERSIONING_SSOT=""
OUT_PATH=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      ROOT_DIR="$2"
      shift 2
      ;;
    --naming-ssot)
      NAMING_SSOT="$2"
      shift 2
      ;;
    --versioning-ssot)
      VERSIONING_SSOT="$2"
      shift 2
      ;;
    --out)
      OUT_PATH="$2"
      shift 2
      ;;
    *)
      echo "Usage: tools/ci/check-versioning-svt.sh [--repo-root <path>] [--naming-ssot <path>] [--versioning-ssot <path>] [--out <path>]" >&2
      exit 2
      ;;
  esac
done

ROOT_DIR="$(cd -- "${ROOT_DIR}" && pwd)"
NAMING_SSOT="${NAMING_SSOT:-${ROOT_DIR}/tools/ci/policies/data/naming.json}"
VERSIONING_SSOT="${VERSIONING_SSOT:-${ROOT_DIR}/tools/ci/policies/data/versioning.json}"
OUT_PATH="${OUT_PATH:-${ROOT_DIR}/artifacts/ci/versioning-svt/versioning-svt-summary.json}"

mkdir -p "${ROOT_DIR}/artifacts" "$(dirname -- "${OUT_PATH}")"

python3 "${ROOT_DIR}/tools/ci/bin/check_versioning_svt.py" "${ROOT_DIR}" "${NAMING_SSOT}" "${VERSIONING_SSOT}" "${OUT_PATH}"
