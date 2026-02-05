#!/usr/bin/env bash
set -euo pipefail

EXCLUDE_BY_FILE='**/*Internals.vb'

dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj \
  -v minimal \
  /p:CollectCoverage=true \
  /p:Include='[FileTypeDetectionLib]*' \
  /p:ExcludeByFile="$EXCLUDE_BY_FILE" \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=90%2c90 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
