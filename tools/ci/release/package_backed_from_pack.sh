#!/usr/bin/env bash
set -euo pipefail

version="${1:?version required}"
project="tests/PackageBacked.Tests/PackageBacked.Tests.csproj"

dotnet restore "${project}" --source artifacts/nuget --source https://api.nuget.org/v3/index.json -p:PackageBackedVersion="${version}" -p:RestoreLockedMode=false --force-evaluate -v minimal
dotnet test "${project}" -c Release --no-restore -p:PackageBackedVersion="${version}" -v minimal
