#!/usr/bin/env bash
set -euo pipefail

# shellcheck source=tools/ci/lib/result.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)/lib/result.sh"

MAX_INLINE_RUN_LINES="${MAX_INLINE_RUN_LINES:-5}"

findings=0

while IFS=: read -r file line _; do
  [[ -z "$file" ]] && continue
  ci_result_add_violation "CI-SHELL-001" "fail" "continue-on-error true is forbidden" "${file}:${line}"
  findings=$((findings + 1))
done < <(rg -n "continue-on-error:\s*true" .github/workflows || true)

while IFS=: read -r file line _; do
  [[ -z "$file" ]] && continue
  ci_result_add_violation "CI-SHELL-002" "fail" "'|| true' is forbidden on critical workflow paths" "${file}:${line}"
  findings=$((findings + 1))
done < <(rg -n "\|\|\s*true" .github/workflows || true)

while IFS=: read -r file line _; do
  [[ -z "$file" ]] && continue
  ci_result_add_violation "CI-SHELL-003" "fail" "'set +e' is forbidden outside documented allow-list" "${file}:${line}"
  findings=$((findings + 1))
done < <(rg -n "^[[:space:]]*set[[:space:]]+\\+e([[:space:]]|$)" .github/workflows tools/ci || true)

while IFS=: read -r file line count; do
  [[ -z "$file" ]] && continue
  ci_result_add_violation "CI-SHELL-004" "fail" "workflow run block exceeds max lines (${MAX_INLINE_RUN_LINES})" "${file}:${line}" "${file}:${count}"
  findings=$((findings + 1))
done < <(awk -v max="$MAX_INLINE_RUN_LINES" '
  function lead_spaces(s,   i,c) {
    c = 0
    for (i = 1; i <= length(s); i++) {
      if (substr(s, i, 1) == " ") c++
      else break
    }
    return c
  }
  BEGIN {inrun=0;count=0;start=0;run_indent=0}
  {
    if ($0 ~ /^[[:space:]]*run:[[:space:]]*\|[[:space:]]*$/) {
      inrun=1
      count=0
      start=NR
      run_indent=lead_spaces($0)
      next
    }
    if (inrun==1) {
      curr_indent=lead_spaces($0)
      if ($0 !~ /^[[:space:]]*$/ && curr_indent <= run_indent) {
        if (count > max) {
          printf "%s:%d:%d\n", FILENAME, start, count
        }
        inrun=0
      } else {
        count++
      }
    }
  }
  END {
    if (inrun==1 && count > max) {
      printf "%s:%d:%d\n", FILENAME, start, count
    }
  }
' .github/workflows/*.yml)

if [[ "$findings" -eq 0 ]]; then
  ci_result_append_summary "Shell safety policy passed."
else
  ci_result_append_summary "Shell safety policy violations: $findings"
  exit 1
fi
