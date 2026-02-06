#!/usr/bin/env bash
set -euo pipefail

EXCLUDE_BY_FILE=''

dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj \
  -v minimal \
  /p:CollectCoverage=true \
  /p:Include='[FileTypeDetectionLib]*' \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=85%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
