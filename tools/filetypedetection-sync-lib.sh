#!/usr/bin/env bash
set -euo pipefail

build_source_manifest() {
  local src_dir="$1"
  local out_file="$2"

  (
    cd "${src_dir}"
    while IFS= read -r -d '' rel; do
      local hash
      hash="$(shasum -a 256 "${rel}" | awk '{print $1}')"
      printf '%s\t%s\n' "${rel#./}" "${hash}"
    done < <(find . \( -name bin -o -name obj \) -prune -o -type f \( -name '*.vb' -o -name '*.md' \) ! -name '*.vbproj' -print0 | LC_ALL=C sort -z)
  ) > "${out_file}"
}

build_portable_manifest() {
  local portable_dir="$1"
  local out_file="$2"

  (
    cd "${portable_dir}"
    while IFS= read -r -d '' rel; do
      local hash
      hash="$(shasum -a 256 "${rel}" | awk '{print $1}')"
      printf '%s\t%s\n' "${rel#./}" "${hash}"
    done < <(find . -type f \( -name '*.vb' -o -name '*.md' \) -print0 | LC_ALL=C sort -z)
  ) > "${out_file}"
}
