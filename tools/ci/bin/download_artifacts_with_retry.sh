#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF' >&2
Usage: download_artifacts_with_retry.sh <run_id> <artifact=dest> [<artifact=dest>...]

Environment:
  GITHUB_REPOSITORY                 Required (owner/repo).
  ARTIFACT_DOWNLOAD_MAX_ATTEMPTS    Optional, default: 6
  ARTIFACT_DOWNLOAD_INITIAL_DELAY   Optional, default: 2 (seconds)
EOF
}

if [[ $# -lt 2 ]]; then
  usage
  exit 2
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI is required." >&2
  exit 1
fi

run_id="$1"
shift

repo="${GITHUB_REPOSITORY:-}"
if [[ -z "${repo}" ]]; then
  echo "ERROR: GITHUB_REPOSITORY is required." >&2
  exit 1
fi

max_attempts="${ARTIFACT_DOWNLOAD_MAX_ATTEMPTS:-6}"
initial_delay="${ARTIFACT_DOWNLOAD_INITIAL_DELAY:-2}"

if [[ ! "${max_attempts}" =~ ^[0-9]+$ || "${max_attempts}" -lt 1 ]]; then
  echo "ERROR: ARTIFACT_DOWNLOAD_MAX_ATTEMPTS must be >= 1." >&2
  exit 1
fi
if [[ ! "${initial_delay}" =~ ^[0-9]+$ || "${initial_delay}" -lt 1 ]]; then
  echo "ERROR: ARTIFACT_DOWNLOAD_INITIAL_DELAY must be >= 1." >&2
  exit 1
fi

download_one() {
  local name="$1"
  local dest="$2"
  local attempt=1
  local delay="${initial_delay}"

  while (( attempt <= max_attempts )); do
    echo "INFO: Download '${name}' attempt ${attempt}/${max_attempts} -> ${dest}"
    rm -rf "${dest}"
    mkdir -p "${dest}"

    if gh run download "${run_id}" --repo "${repo}" -n "${name}" -D "${dest}" >/tmp/artifact-download.log 2>&1; then
      return 0
    fi

    if (( attempt == max_attempts )); then
      echo "ERROR: Download '${name}' failed after ${max_attempts} attempts." >&2
      cat /tmp/artifact-download.log >&2 || true
      return 1
    fi

    echo "WARN: Download '${name}' failed. Retrying in ${delay}s." >&2
    cat /tmp/artifact-download.log >&2 || true
    sleep "${delay}"
    delay=$((delay * 2))
    attempt=$((attempt + 1))
  done

  return 1
}

for spec in "$@"; do
  if [[ "${spec}" != *=* ]]; then
    echo "ERROR: Invalid artifact mapping '${spec}'. Expected <artifact=dest>." >&2
    exit 1
  fi
  name="${spec%%=*}"
  dest="${spec#*=}"
  if [[ -z "${name}" || -z "${dest}" ]]; then
    echo "ERROR: Invalid artifact mapping '${spec}'. Expected non-empty name and destination." >&2
    exit 1
  fi

  download_one "${name}" "${dest}"
done

echo "OK: all artifacts downloaded with retry policy."
