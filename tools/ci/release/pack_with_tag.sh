#!/usr/bin/env bash
set -euo pipefail

version="${1:?version required}"
tag="${2:?tag required}"
assembly_version="${3:?assembly version required}"
file_version="${4:?file version required}"

mkdir -p artifacts/nuget
dotnet pack src/FileTypeDetection/FileTypeDetectionLib.vbproj \
  -c Release \
  --no-build \
  -o artifacts/nuget \
  -p:PackageVersion="${version}" \
  -p:Version="${version}" \
  -p:ContinuousIntegrationBuild=true \
  -p:RepositoryTag="${tag}" \
  -p:AssemblyVersion="${assembly_version}" \
  -p:FileVersion="${file_version}"
