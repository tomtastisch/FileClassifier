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

python3 - "$trx_file" <<'PY' | tee "${readable_report}"
import re
import sys
import xml.etree.ElementTree as ET

BLUE = "\033[94m"
WHITE = "\033[97m"
GREEN = "\033[32m"
RED = "\033[31m"
RESET = "\033[0m"
DIM = "\033[2m"

CHECK = "✔"
CROSS = "✘"

trx_path = sys.argv[1]
root = ET.parse(trx_path).getroot()
ns = {"t": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}

def findall(path):
    return root.findall(path, ns) if ns else root.findall(path)

def find(node, path):
    return node.find(path, ns) if ns else node.find(path)

def strip_param_suffix(text: str) -> str:
    value = text.strip()
    # Drop trailing "(...)" blocks used by data-driven test display names.
    while True:
        updated = re.sub(r"\s*\([^()]*\)\s*$", "", value).strip()
        if updated == value:
            return value
        value = updated

def humanize_identifier(text: str) -> str:
    value = strip_param_suffix(text)
    value = value.replace("_", " ")
    value = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", value)
    value = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1 \2", value)
    value = re.sub(r"\s+", " ", value).strip()
    return value

def normalize_title(test_name: str, scenario: str | None) -> str:
    if scenario:
        return strip_param_suffix(scenario)

    raw = strip_param_suffix(test_name)
    # xUnit names are usually Namespace.Class.Method(...). Keep only method part.
    if "." in raw:
        raw = raw.rsplit(".", 1)[-1]
    return humanize_identifier(raw)

def iter_step_lines(stdout: str):
    if not stdout:
        return []
    lines = []
    for raw in stdout.splitlines():
        line = raw.strip()
        if not line:
            continue
        if line.startswith("[BDD]"):
            continue
        if line.startswith("-> done:"):
            continue
        if line.startswith("--- table step argument ---"):
            continue
        if line.startswith("|"):
            continue
        if line.startswith("Standardausgabemeldungen:"):
            continue
        if re.match(r"^(Angenommen|Wenn|Dann|Und|Aber)\b", line):
            lines.append(line)
    # dedupe preserving order
    deduped = []
    seen = set()
    for line in lines:
        if line not in seen:
            deduped.append(line)
            seen.add(line)
    return deduped

results = []
for node in findall(".//t:UnitTestResult" if ns else ".//UnitTestResult"):
    outcome = (node.attrib.get("outcome") or "").strip()
    test_name = (node.attrib.get("testName") or "").strip()
    output = find(node, "t:Output" if ns else "Output")
    stdout = ""
    if output is not None:
        std_node = find(output, "t:StdOut" if ns else "StdOut")
        if std_node is not None and std_node.text:
            stdout = std_node.text

    scenario = None
    if stdout:
        for line in stdout.splitlines():
            l = line.strip()
            m = re.match(r"^\[BDD\]\s*Szenario startet:\s*(.+)$", l)
            if m:
                scenario = m.group(1).strip()
                break

    title = normalize_title(test_name, scenario)
    steps = iter_step_lines(stdout)
    results.append((title, outcome, steps))

for title, outcome, steps in results:
    passed = outcome.lower() == "passed"
    icon = CHECK if passed else CROSS
    icon_color = GREEN if passed else RED
    end_word = "FINISHED" if passed else "FAILED"

    if not steps:
        steps = ["Test erfolgreich abgeschlossen" if passed else "Test fehlgeschlagen"]

    print(f"{DIM}────────────────────────────────────────────────────────────────{RESET}")
    print(f"{BLUE}{title}{RESET}")
    for s in steps:
        print(f"{icon_color}{icon}{RESET} {WHITE}{s}{RESET}")
    print(f"{icon_color}{end_word}{RESET}")
    print("")
PY

exit "${test_exit}"
