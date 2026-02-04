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
  Runs all tests from FileClassifier.sln with detailed console output.

Filters:
  --materializer          Runs only BDD scenarios tagged @materializer.
  --materializer-negative Runs only BDD scenarios tagged @materializer and @negative.
USAGE
}

mode="${1:-}"
case "${mode}" in
  -h|--help)
    usage
    exit 0
    ;;
  --materializer)
    shift
    dotnet test "${TEST_PROJECT}" \
      --logger "console;verbosity=detailed" \
      --filter "Category=materializer" \
      "$@"
    ;;
  --materializer-negative)
    shift
    dotnet test "${TEST_PROJECT}" \
      --logger "console;verbosity=detailed" \
      --filter "Category=materializer&Category=negative" \
      "$@"
    ;;
  *)
    dotnet test "${SOLUTION_FILE}" \
      --logger "console;verbosity=detailed" \
      "$@"
    ;;
esac
