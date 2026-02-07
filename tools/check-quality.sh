#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

python3 tools/check-docs.py

dotnet list FileClassifier.sln package --vulnerable --include-transitive

dotnet build FileClassifier.sln -c Release --no-restore

bash tools/run-coverage.sh

TEST_BDD_OUTPUT_DIR="${ROOT_DIR}/artifacts/test-bdd" \
  bash tools/test-bdd-readable.sh -- -c Release --no-restore

if [[ -z "${QODANA_TOKEN:-}" ]]; then
  echo "ERROR: QODANA_TOKEN is not set."
  exit 1
fi

qodana scan \
  --config qodana.yaml \
  --image jetbrains/qodana-dotnet:2025.3 \
  --results-dir qodana-results \
  -e QODANA_TOKEN="$QODANA_TOKEN"
