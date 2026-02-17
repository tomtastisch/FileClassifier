# Evidence Report: netstandard2 Compat

## 1. Zweck
Dieser Report dokumentiert die technische Umsetzung und Verifikation fuer die net48-Kompatibilitaet ueber `netstandard2.0`.

## 2. Geltungsbereich
- Library: `src/FileTypeDetection/FileTypeDetectionLib.vbproj`
- Hashing-Core-Fassade: `src/FileTypeDetection/EvidenceHashing.vb`
- Provider-Abstraktionen und TFM-Provider unter `src/FileTypeDetection/Abstractions/Providers`, `src/FileTypeDetection/Composition`, `src/FileTypeDetection/Providers`

## 3. Regeln/Architektur
### 3.1 Before/After TargetFrameworks
- Before: `net8.0;net10.0`
- After: `netstandard2.0;net8.0;net10.0`
- Nachweisdatei: `src/FileTypeDetection/FileTypeDetectionLib.vbproj`

### 3.2 Interface-Abstraktionen und API-Mapping
- `IHexCodec`
  - abstrahiert: Lower-Hex-Encoding (`Convert.ToHexString(...).ToLowerInvariant()` bzw. nibble-map)
- `ISha256Primitives`
  - abstrahiert: `SHA256.HashData`/`SHA256.Create().ComputeHash`
- `IFastHash64`
  - abstrahiert: `System.IO.Hashing.XxHash3.HashToUInt64`
- `IHashPrimitives`
  - Aggregation + `ProviderMarker`

### 3.3 Hashing-Verhalten je TFM
- `netstandard2.0`:
  - SHA256: `SHA256.Create()`
  - Hex: deterministischer lower-hex nibble-codec
  - FastHash: `XxHash3.HashToUInt64(...).ToString("x16")`
- `net8.0` und `net10.0`:
  - SHA256: `SHA256.HashData`
  - Hex: `Convert.ToHexString(...).ToLowerInvariant()`
  - FastHash: `XxHash3.HashToUInt64(...).ToString("x16")`
- FastHash ist auf `netstandard2.0` **nicht** deaktiviert.

### 3.4 Provider-Selektion (compile-time)
MSBuild-Conditionen in `src/FileTypeDetection/FileTypeDetectionLib.vbproj`:
- global: `<Compile Remove="Providers/**/*.vb"/>`
- `netstandard2.0`: `<Compile Include="Providers/NetStandard2_0/**/*.vb"/>`
- `net8.0|net10.0`: `<Compile Include="Providers/Net8_0Plus/**/*.vb"/>`

## 4. Verifikation/Nachweise
### 4.1 Befehle und Exit-Codes
1. `dotnet --info` -> `0`
2. `dotnet restore FileClassifier.sln -v minimal` -> `0`
3. `dotnet build FileClassifier.sln -c Release --no-restore -warnaserror -v minimal` -> `0`
4. `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release --no-build -v minimal` -> `0` (`414` Tests gruen)
5. `dotnet pack src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release --no-build -o artifacts/ci/netstandard2-compat/nuget -v minimal` -> `0`
6. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f netstandard2.0 -v diag > artifacts/ci/netstandard2-compat/build-netstandard2.0.log` -> `0`
7. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f net8.0 -v diag > artifacts/ci/netstandard2-compat/build-net8.0.log` -> `0`
8. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f net10.0 -v diag > artifacts/ci/netstandard2-compat/build-net10.0.log` -> `0`
9. `python3 tools/check-doc-consistency.py` -> `0`
10. `python3 tools/check-docs.py` -> `0`
11. `bash tools/versioning/verify-version-convergence.sh` -> `0`
12. `bash tools/ci/bin/run.sh security-nuget` -> `0`
13. `EXPECTED_RELEASE_TAG=v5.2.0-rc.3 REQUIRE_RELEASE_TAG=1 bash tools/ci/check-versioning-svt.sh --repo-root . --out artifacts/ci/versioning-svt/versioning-svt-summary.json` -> `0`
14. `bash tools/ci/release/gate2_version_policy.sh release v5.2.0-rc.3 artifacts/nuget/Tomtastisch.FileClassifier.5.2.0-rc.3.nupkg` -> `0`
15. `VERIFY_ONLINE=0 bash tools/ci/release/gate4_verify_postpublish.sh 5.2.0-rc.3 artifacts/nuget/Tomtastisch.FileClassifier.5.2.0-rc.3.nupkg` -> `0`
16. `VERIFY_ONLINE=0 bash tools/ci/release/gate4_verify_postpublish.sh 5.2.0 artifacts/ci/netstandard2-compat/nuget/Tomtastisch.FileClassifier.5.2.0.nupkg` -> `0`

### 4.2 Build-/Pack-Proof
- Build-Matrix erfolgreich:
  - `src/FileTypeDetection/bin/Release/netstandard2.0/Tomtastisch.FileClassifier.dll`
  - `src/FileTypeDetection/bin/Release/net8.0/Tomtastisch.FileClassifier.dll`
  - `src/FileTypeDetection/bin/Release/net10.0/Tomtastisch.FileClassifier.dll`
- NUPKG-Inhalt (`unzip -l ... | rg "lib/"`):
  - `lib/netstandard2.0/Tomtastisch.FileClassifier.dll`
  - `lib/net8.0/Tomtastisch.FileClassifier.dll`
  - `lib/net10.0/Tomtastisch.FileClassifier.dll`

### 4.3 Provider-Compile-Proof
- Build-Logs enthalten die erwarteten Providerpfade je TFM:
  - `artifacts/ci/netstandard2-compat/build-netstandard2.0.log` mit `Providers/NetStandard2_0/HashPrimitivesProvider.vb`
  - `artifacts/ci/netstandard2-compat/build-net8.0.log` mit `Providers/Net8_0Plus/HashPrimitivesProvider.vb`
  - `artifacts/ci/netstandard2-compat/build-net10.0.log` mit `Providers/Net8_0Plus/HashPrimitivesProvider.vb`
- Runtime-nahe Marker-Probe aus den drei Build-Artefakten:
  - `netstandard2.0:NetStandard2_0`
  - `net8.0:Net8_0Plus`
  - `net10.0:Net8_0Plus`
- Probe-Kommando:
```bash
tmpdir=$(mktemp -d)
cd "$tmpdir"
dotnet new console -n Probe -f net10.0
# Program.cs laedt jede TFM-DLL in eigenem AssemblyLoadContext und liest ProviderMarker via Reflection.
dotnet run -c Release --no-restore
```

### 4.4 Forbidden-API Grep-Proof (Core)
Befehl:
```bash
rg -n "Convert\.ToHexString|SHA256\.HashData|System\.IO\.Hashing|Microsoft\.AspNetCore\.App" src/FileTypeDetection/Core
```
Ergebnis:
- keine Treffer (`forbidden_core_refs=none`)

### 4.5 CI-Teilchecks
- `artifacts/ci/versioning-svt/versioning-svt-summary.json` -> `status: pass` (pre-release `v5.2.0-rc.3`, core-match `5.2.0`)
- `artifacts/ci/version-convergence/summary.json` -> `status: pass`, `repo_version=5.2.0`, `vbproj_version=5.2.0`, `docs_latest_version=5.2.0`
- `artifacts/ci/security-nuget/result.json` -> `status: pass`
- Gate-4-PreRelease-Probe (`VERIFY_ONLINE=0`) zeigt `require_registration=0`.
- Gate-4-Stable-Probe (`VERIFY_ONLINE=0`) zeigt `require_registration=1`.

### 4.6 Policy/Konvergenz-Notiz
Ambiguitaet zwischen:
- `docs/versioning/001_POLICY_VERSIONING.MD:43` (in PR/CI keine statischen Versionfelder), und
- existierendem SVT/Convergence-Setup (`verify-version-convergence.sh`, `check-versioning-svt.sh`), das `RepoVersion` und `Version`/`PackageVersion` in `FileTypeDetectionLib.vbproj` erwartet.

Entscheidung fuer diesen Scope:
- fail-closed nach bestehendem CI/Repo-Vertrag: Versionen auf `5.2.0` synchron gehalten und durch `versioning-svt` + `version-convergence` verifiziert.
- Pre-Releases werden ueber Tag `v5.2.0-rc.N` abgebildet; die Projektfelder bleiben semantisch auf Kernversion `5.2.0`.

## 5. Grenzen/Nicht-Ziele
- Keine oeffentliche API-Signatur geaendert.
- Keine Runtime-Provider-Umschaltung eingefuehrt.
- Keine Blocker-Datei notwendig (`999_NETSTANDARD2_BLOCKERS.MD` nicht erstellt).

## 6. Verlinkte SSOT-Quellen
- [Versioning Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/001_POLICY_VERSIONING.MD)
- [Code Quality Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
- [Documentation Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/004_POLICY_DOCUMENTATION.MD)
- [Audit Index](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/000_INDEX.MD)
