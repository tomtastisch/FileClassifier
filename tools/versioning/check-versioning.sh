#!/usr/bin/env bash
set -euo pipefail

BASE_REF="${BASE_REF:-origin/main}"
HEAD_REF="${HEAD_REF:-HEAD}"
MODE="${MODE:-check}"

# Fetch base and tags if available.
git fetch --quiet origin main --tags >/dev/null 2>&1 || true

if git show-ref --verify --quiet "refs/remotes/${BASE_REF}"; then
  BASE="${BASE_REF}"
elif git rev-parse --verify --quiet "${BASE_REF}" >/dev/null; then
  BASE="${BASE_REF}"
elif git rev-parse --verify --quiet HEAD~1 >/dev/null; then
  BASE="HEAD~1"
else
  BASE="HEAD"
fi

if ! changes=$(git diff --name-status "${BASE}"..."${HEAD_REF}" 2>/dev/null); then
  # No merge base (e.g., bot branches). Default to none for labeling, fail closed only in CI guard.
  if [[ "${MODE}" == "required" ]]; then
    echo "none"
    exit 0
  fi
  echo "versioning-guard: unable to compute diff between ${BASE} and ${HEAD_REF}" >&2
  exit 5
fi

has_any_change=0
has_code_change=0
has_api_docs_change=0
has_breaking_rename_delete=0

is_public_path() {
  case "$1" in
    src/FileTypeDetection/*|src/FileClassifier.App/*) return 0;;
    *) return 1;;
  esac
}

is_api_doc() {
  case "$1" in
    docs/01_FUNCTIONS.md|docs/03_REFERENCES.md|docs/04_DETERMINISTIC_HASHING_API_CONTRACT.md) return 0;;
    *) return 1;;
  esac
}

while IFS=$'\t' read -r status a b; do
  [[ -z "${status}" ]] && continue
  has_any_change=1

  if [[ "${status}" == R* ]]; then
    # rename: status, old, new
    if is_public_path "${a}" || is_public_path "${b}"; then
      has_breaking_rename_delete=1
    fi
    if is_public_path "${a}" || is_public_path "${b}"; then
      has_code_change=1
    fi
    if is_api_doc "${a}" || is_api_doc "${b}"; then
      has_api_docs_change=1
    fi
    continue
  fi

  if [[ "${status}" == D ]]; then
    if is_public_path "${a}"; then
      has_breaking_rename_delete=1
      has_code_change=1
    fi
    if is_api_doc "${a}"; then
      has_api_docs_change=1
    fi
    continue
  fi

  # Added/Modified/etc
  if is_public_path "${a}"; then
    has_code_change=1
  fi
  if is_api_doc "${a}"; then
    has_api_docs_change=1
  fi

done <<< "${changes}"

breaking_changelog=0
if [[ -f docs/versioning/CHANGELOG.md ]]; then
  in_unreleased=0
  while IFS= read -r line; do
    if [[ "${line}" =~ ^##\ \[Unreleased\] ]]; then
      in_unreleased=1
      continue
    fi
    if [[ "${line}" =~ ^##\ \[ ]]; then
      in_unreleased=0
    fi
    if [[ ${in_unreleased} -eq 1 ]] && [[ "${line}" == *"BREAKING:"* ]]; then
      breaking_changelog=1
      break
    fi
  done < docs/versioning/CHANGELOG.md
fi

required="none"
if [[ ${has_any_change} -eq 0 ]]; then
  required="none"
elif [[ ${breaking_changelog} -eq 1 || ${has_breaking_rename_delete} -eq 1 ]]; then
  required="major"
elif [[ ${has_code_change} -eq 1 || ${has_api_docs_change} -eq 1 ]]; then
  required="minor"
else
  required="patch"
fi

if [[ "${MODE}" == "required" ]]; then
  echo "${required}"
  exit 0
fi

# Determine base version from latest tag or base file
base_version=""
latest_tag=$(git tag -l 'v[0-9]*' --sort=-v:refname | head -n1)
if [[ -n "${latest_tag}" ]]; then
  base_version="${latest_tag#v}"
else
  if git show "${BASE}:Directory.Build.props" >/dev/null 2>&1; then
    base_version=$(git show "${BASE}:Directory.Build.props" | sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' | head -n1)
  fi
fi

current_version=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' Directory.Build.props | head -n1)

if [[ -z "${base_version}" || -z "${current_version}" ]]; then
  echo "versioning-guard: unable to parse versions (base='${base_version}', current='${current_version}')" >&2
  exit 2
fi

parse_version() {
  IFS='.' read -r major minor patch <<< "$1"
  echo "${major:-0} ${minor:-0} ${patch:-0}"
}

read -r bmaj bmin bpat < <(parse_version "${base_version}")
read -r cmaj cmin cpat < <(parse_version "${current_version}")

actual="none"
if [[ ${cmaj} -gt ${bmaj} ]]; then
  actual="major"
elif [[ ${cmaj} -eq ${bmaj} && ${cmin} -gt ${bmin} ]]; then
  actual="minor"
elif [[ ${cmaj} -eq ${bmaj} && ${cmin} -eq ${bmin} && ${cpat} -gt ${bpat} ]]; then
  actual="patch"
elif [[ ${cmaj} -eq ${bmaj} && ${cmin} -eq ${bmin} && ${cpat} -eq ${bpat} ]]; then
  actual="none"
else
  echo "versioning-guard: current version ${current_version} is not >= base ${base_version}" >&2
  exit 3
fi

rank() {
  case "$1" in
    none) echo 0;;
    patch) echo 1;;
    minor) echo 2;;
    major) echo 3;;
    *) echo 0;;
  esac
}

req_rank=$(rank "${required}")
act_rank=$(rank "${actual}")

cat <<MSG
versioning-guard: base=${base_version} current=${current_version}
versioning-guard: required=${required} actual=${actual}
MSG

if [[ ${act_rank} -lt ${req_rank} ]]; then
  echo "versioning-guard: version bump is missing or too small" >&2
  exit 4
fi

if [[ ${act_rank} -gt ${req_rank} && ${req_rank} -ne 0 ]]; then
  echo "versioning-guard: warning: bump larger than required" >&2
fi

exit 0
