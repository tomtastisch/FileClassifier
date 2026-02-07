#!/usr/bin/env bash
set -euo pipefail

# Ensure Reqnroll feature code is regenerated for Release and stale outputs cannot mask errors.
dotnet clean tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release >/dev/null

dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj \
  -c Release \
  -v minimal \
  --no-restore \
  /p:CollectCoverage=true \
  /p:Include='[FileTypeDetectionLib]*' \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=85%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
