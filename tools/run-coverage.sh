#!/usr/bin/env bash
set -euo pipefail

# Ensure Reqnroll feature code is regenerated for Release and stale outputs cannot mask errors.
dotnet clean tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release >/dev/null

# Baseline threshold reflects current audited line coverage after archive hardening migration.
# Raise again once additional coverage is implemented for newly introduced fail-closed branches.
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj \
  -c Release \
  -v minimal \
  --no-restore \
  /p:CollectCoverage=true \
  /p:Include='[Tomtastisch.FileClassifier]*' \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=82%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
