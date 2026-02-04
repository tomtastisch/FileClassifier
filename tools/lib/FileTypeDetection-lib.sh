#!/usr/bin/env bash
set -euo pipefail

# ------------------------------------------------------------
# FileTypeDetection Tools Library
# - deterministische Manifeste (relpath<TAB>sha256)
# - portable sync/validate helpers
# - robuste Pfad-/Hash-Funktionen für macOS/Linux
# ------------------------------------------------------------

ftd_die() { echo "FAIL  $*" >&2; exit 1; }
ftd_info() { echo "INFO  $*" >&2; }
ftd_ok() { echo "OK    $*" >&2; }

ftd_require_cmd() {
  local cmd="$1"
  command -v "$cmd" >/dev/null 2>&1 || ftd_die "Missing required command: ${cmd}"
}

ftd_repo_root_from_script() {
  # Erwartet: tools/ oder tools/lib/ als Script-Location.
  # Gibt repo root (.. oder ../..) zurück, je nachdem wo das Script liegt.
  local script_dir
  script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
  if [[ -d "${script_dir}/../src" ]]; then
    (cd "${script_dir}/.." && pwd -P)
  elif [[ -d "${script_dir}/../../src" ]]; then
    (cd "${script_dir}/../.." && pwd -P)
  else
    ftd_die "Cannot resolve repo root from: ${script_dir}"
  fi
}

ftd_sha256_file() {
  local file="$1"
  if command -v shasum >/dev/null 2>&1; then
    shasum -a 256 "$file" | awk '{print $1}'
  elif command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$file" | awk '{print $1}'
  elif command -v openssl >/dev/null 2>&1; then
    openssl dgst -sha256 "$file" | awk '{print $2}'
  else
    ftd_die "No sha256 tool found (shasum/sha256sum/openssl)."
  fi
}

ftd_build_manifest() {
  # Args:
  #   $1 = directory
  #   $2 = output file
  #   $3 = mode: "source"|"portable"
  #
  # source:  *.vb + *.md, exclude bin/obj, exclude *.vbproj
  # portable: *.vb + *.md (portable ist bereits "clean")
  local dir="$1"
  local out_file="$2"
  local mode="${3:-source}"

  [[ -d "$dir" ]] || ftd_die "Directory not found: $dir"

  (
    cd "$dir"

    local find_expr=()
    if [[ "$mode" == "source" ]]; then
      find_expr=(
        . \( -name bin -o -name obj \) -prune -o
      -type f \( -name '*.vb' -o -name '*.md' \)
      ! -name '*.vbproj' -print0
      )
    elif [[ "$mode" == "portable" ]]; then
      find_expr=(
        . -type f \( -name '*.vb' -o -name '*.md' \) -print0
      )
    else
      ftd_die "Unknown manifest mode: $mode"
    fi

    while IFS= read -r -d '' rel; do
      local hash
      hash="$(ftd_sha256_file "${rel}")"
      printf '%s\t%s\n' "${rel#./}" "${hash}"
    done < <(find "${find_expr[@]}" | LC_ALL=C sort -z)
  ) > "$out_file"
}

ftd_assert_safe_rmrf_target() {
  # Verhindert rm -rf auf leeren/unerwarteten Pfaden.
  # Args:
  #   $1 = target dir
  #   $2 = must_contain (Substring, z.B. "/portable/FileTypeDetection")
  local target="$1"
  local must_contain="$2"

  [[ -n "${target}" ]] || ftd_die "Refusing rm -rf: empty target"
  [[ "${target}" != "/" ]] || ftd_die "Refusing rm -rf: target is /"
  [[ "${target}" == *"${must_contain}"* ]] || ftd_die "Refusing rm -rf: target does not contain '${must_contain}': ${target}"
}

ftd_detect_first_tfm() {
  # Best-effort: liest erstes TargetFramework(s) aus src/*.csproj/vbproj.
  # Fallback: net10.0
  local root="$1"
  local tfm=""
  local candidate=""

  # Prefer TargetFrameworks then TargetFramework; accept first occurrence.
  candidate="$(grep -R --line-number -h -E '<TargetFrameworks>|<TargetFramework>' \
    "${root}/src" 2>/dev/null | head -n 1 || true)"

  if [[ -n "${candidate}" ]]; then
    if [[ "${candidate}" == *"<TargetFrameworks>"* ]]; then
      tfm="$(echo "${candidate}" | sed -n 's/.*<TargetFrameworks>\([^<]*\).*/\1/p' | awk -F';' '{print $1}')"
    else
      tfm="$(echo "${candidate}" | sed -n 's/.*<TargetFramework>\([^<]*\).*/\1/p')"
    fi
  fi

  if [[ -z "${tfm}" ]]; then
    tfm="net10.0"
  fi

  echo "${tfm}"
}