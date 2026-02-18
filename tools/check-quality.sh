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

dotnet test tests/PackageBacked.Tests/PackageBacked.Tests.csproj \
  -c Release \
  --no-restore \
  -f net10.0

run_qodana=true
if [[ "${SKIP_QODANA:-0}" == "1" ]]; then
  echo "INFO: SKIP_QODANA=1 -> Qodana wird uebersprungen."
  run_qodana=false
fi

if [[ "${run_qodana}" == "true" ]] && ! command -v qodana >/dev/null 2>&1; then
  if [[ "${CI:-false}" == "true" ]]; then
    echo "ERROR: qodana CLI nicht gefunden."
    exit 1
  fi
  echo "WARN: qodana CLI nicht gefunden -> Qodana wird lokal uebersprungen."
  run_qodana=false
fi

if [[ "${run_qodana}" == "true" ]] && [[ -z "${QODANA_TOKEN:-}" ]]; then
  if [[ "${CI:-false}" == "true" ]]; then
    echo "ERROR: QODANA_TOKEN is not set."
    exit 1
  fi
  echo "WARN: QODANA_TOKEN fehlt -> Qodana wird lokal uebersprungen."
  run_qodana=false
fi

if [[ "${run_qodana}" == "true" ]] && ! docker info >/dev/null 2>&1; then
  if [[ "${CI:-false}" == "true" ]]; then
    echo "ERROR: Docker daemon nicht erreichbar; Qodana kann nicht ausgefuehrt werden."
    exit 1
  fi
  echo "WARN: Docker daemon nicht erreichbar -> Qodana wird lokal uebersprungen."
  run_qodana=false
fi

if [[ "${run_qodana}" == "true" ]]; then
  qodana scan \
    --config qodana.yaml \
    --image jetbrains/qodana-dotnet:2025.3 \
    --results-dir qodana-results \
    -e QODANA_TOKEN="$QODANA_TOKEN"
fi
