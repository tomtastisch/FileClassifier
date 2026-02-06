#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

python3 tools/check-docs.py

dotnet list FileClassifier.sln package --vulnerable

dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj \
  -v minimal \
  /p:CollectCoverage=true \
  /p:Include="[FileTypeDetectionLib]*" \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=85%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total

if [[ -z "${QODANA_TOKEN:-}" ]]; then
  echo "ERROR: QODANA_TOKEN is not set."
  exit 1
fi

qodana scan \
  --config qodana.yaml \
  --image jetbrains/qodana-dotnet:2025.3 \
  --results-dir qodana-results \
  -e QODANA_TOKEN="$QODANA_TOKEN"
