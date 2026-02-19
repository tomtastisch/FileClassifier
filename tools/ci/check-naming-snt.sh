#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'
LC_ALL=C

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
SSOT_PATH="${ROOT_DIR}/tools/ci/policies/data/naming.json"
OUT_PATH="${ROOT_DIR}/artifacts/ci/naming-snt/naming-snt-summary.json"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      ROOT_DIR="$2"
      shift 2
      ;;
    --ssot)
      SSOT_PATH="$2"
      shift 2
      ;;
    --out)
      OUT_PATH="$2"
      shift 2
      ;;
    *)
      echo "Usage: tools/ci/check-naming-snt.sh [--repo-root <path>] [--ssot <path>] [--out <path>]" >&2
      exit 2
      ;;
  esac
done

mkdir -p "$(dirname -- "${OUT_PATH}")" "${ROOT_DIR}/artifacts"

python3 "${ROOT_DIR}/tools/ci/bin/check_naming_snt.py" "${ROOT_DIR}" "${SSOT_PATH}" "${OUT_PATH}"
