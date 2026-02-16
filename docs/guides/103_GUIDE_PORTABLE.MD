# Portable Adoption Guide (NuGet)

## 1. Goal
This guide describes portable adoption as a NuGet package without `ProjectReference`.

## 2. Package Data
- PackageId: `Tomtastisch.FileClassifier`
- Feed: NuGet.org
- Versioning source: Git tag `vX.Y.Z` (optional `-prerelease`) as SSOT

## 3. Consumer Setup
```xml
<ItemGroup>
  <PackageReference Include="Tomtastisch.FileClassifier" Version="x.y.z" />
</ItemGroup>
```

## 4. Verification path (repo-local)
1. `dotnet pack src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -o artifacts/nuget -v minimal`
2. `dotnet restore samples/PortableConsumer/PortableConsumer.csproj --configfile samples/PortableConsumer/NuGet.config -p:PortableConsumerPackageVersion=x.y.z -v minimal`
3. `dotnet build samples/PortableConsumer/PortableConsumer.csproj -c Release --no-restore -p:PortableConsumerPackageVersion=x.y.z -v minimal`
4. `dotnet run --project samples/PortableConsumer/PortableConsumer.csproj -c Release --no-build -p:PortableConsumerPackageVersion=x.y.z`
5. `dotnet restore tests/PackageBacked.Tests/PackageBacked.Tests.csproj --configfile tests/PackageBacked.Tests/NuGet.config -p:PackageBackedVersion=x.y.z -v minimal`
6. `dotnet test tests/PackageBacked.Tests/PackageBacked.Tests.csproj -c Release --no-restore -p:PackageBackedVersion=x.y.z -v minimal`

## 5. Core invariants for \"1:1 identical\"
- API identity: public API snapshot remains unchanged (`tests/FileTypeDetectionLib.Tests/Contracts/public-api.snapshot.txt`).
- Behavioral identity: `tests/PackageBacked.Tests` validates core cases against the packed package:
  - `Detect_And_Hash_Contracts_RemainStable`
  - `Materializer_Persists_Bytes_FailClosedContract`
- Consumer smoke: `samples/PortableConsumer` builds and runs only via `PackageReference`.

## 6. Troubleshooting
- Error `NU1101`: package version not found in feed -> check version/feed in `NuGet.config`.
- Error `NUGET_API_KEY ... placeholder`: set repo secret `NUGET_API_KEY` to a real NuGet.org API key.
