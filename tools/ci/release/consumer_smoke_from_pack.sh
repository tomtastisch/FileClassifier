#!/usr/bin/env bash
set -euo pipefail

version="${1:?version required}"
project="samples/PortableConsumer/PortableConsumer.csproj"

dotnet restore "${project}" --source artifacts/nuget --source https://api.nuget.org/v3/index.json -p:PortableConsumerPackageVersion="${version}" -p:RestoreLockedMode=false --force-evaluate -v minimal
dotnet build "${project}" -c Release --no-restore -p:PortableConsumerPackageVersion="${version}" -v minimal
dotnet run --project "${project}" -c Release -f net10.0 --no-build -p:PortableConsumerPackageVersion="${version}"
