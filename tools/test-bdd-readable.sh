#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
SOLUTION_FILE="${ROOT_DIR}/FileClassifier.sln"
TEST_PROJECT="${ROOT_DIR}/tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj"

usage() {
  cat <<'USAGE'
Usage:
  bash tools/test-bdd-readable.sh [dotnet test args...]
  bash tools/test-bdd-readable.sh --materializer
  bash tools/test-bdd-readable.sh --materializer-negative

Default:
  Runs all tests from FileClassifier.sln and renders a readable, per-test report.

Filters:
  --materializer          Runs only BDD scenarios tagged @materializer.
  --materializer-negative Runs only BDD scenarios tagged @materializer and @negativ.
USAGE
}

mode="${1:-}"
target="${SOLUTION_FILE}"
filter=""
extra_args=()

case "${mode}" in
  -h|--help)
    usage
    exit 0
    ;;
  --materializer)
    shift
    target="${TEST_PROJECT}"
    filter="Category=materializer"
    ;;
  --materializer-negative)
    shift
    target="${TEST_PROJECT}"
    filter="Category=materializer&Category=negativ"
    ;;
esac

if [[ "${1:-}" == "--" ]]; then
  shift
fi

if [[ $# -gt 0 ]]; then
  extra_args=("$@")
fi

if [[ -n "${TEST_BDD_OUTPUT_DIR:-}" ]]; then
  tmp_dir="${TEST_BDD_OUTPUT_DIR}"
  mkdir -p "${tmp_dir}"
else
  tmp_dir="$(mktemp -d)"
  trap 'rm -rf "${tmp_dir}"' EXIT
fi
trx_file="${tmp_dir}/results.trx"
raw_log="${tmp_dir}/dotnet-test.log"
readable_report="${tmp_dir}/bdd-readable.txt"
coverage_dir="${ROOT_DIR}/artifacts/coverage"
coverage_file="${coverage_dir}/coverage.cobertura.xml"
coverage_summary="${coverage_dir}/coverage-summary.txt"

cmd=(
  dotnet test "${target}"
  --logger "trx;LogFileName=$(basename "${trx_file}")"
  --results-directory "${tmp_dir}"
)

if [[ -n "${filter}" ]]; then
  cmd+=(--filter "${filter}")
fi

if [[ ${#extra_args[@]} -gt 0 ]]; then
  cmd+=("${extra_args[@]}")
fi

set +e
"${cmd[@]}" >"${raw_log}" 2>&1
test_exit=$?
set -e

if [[ ! -f "${trx_file}" ]]; then
  found_trx="$(find "${tmp_dir}" -maxdepth 2 -type f -name "*.trx" | head -n1 || true)"
  if [[ -n "${found_trx}" ]]; then
    trx_file="${found_trx}"
  fi
fi

if [[ ! -f "${trx_file}" ]]; then
  printf 'No TRX results found. dotnet output:\n'
  cat "${raw_log}"
  exit "${test_exit}"
fi

mkdir -p "${coverage_dir}"
{
  echo "Coverage summary (from dotnet test output):"
  grep -E "^\| Total\s+\|" "${raw_log}" || true
  if [[ -f "${coverage_file}" ]]; then
    echo "Coverage file: ${coverage_file}"
  else
    echo "Coverage file: not found (${coverage_file})"
  fi
} >"${coverage_summary}"

python3 "${ROOT_DIR}/tools/ci/bin/bdd_readable_from_trx.py" "${trx_file}" | tee "${readable_report}"

exit "${test_exit}"
