# Evidence Report: netstandard2 Compat

## 1. Zweck
Dieser Report dokumentiert die technische Umsetzung und Verifikation fuer die net48-Kompatibilitaet ueber `netstandard2.0` inklusive fail-closed Office-/Archiv-Refinement.

## 2. Geltungsbereich
- Library: `src/FileTypeDetection/FileTypeDetectionLib.vbproj`
- Detektion/Refinement: `src/FileTypeDetection/FileTypeDetector.vb`, `src/FileTypeDetection/Detection/FileTypeRegistry.vb`, `src/FileTypeDetection/Infrastructure/CoreInternals.vb`
- Tests: `tests/FileTypeDetectionLib.Tests/Unit/*`

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
- FastHash ist auf `netstandard2.0` nicht deaktiviert.

### 3.4 Provider-Selektion (compile-time)
MSBuild-Conditionen in `src/FileTypeDetection/FileTypeDetectionLib.vbproj`:
- global: `<Compile Remove="Providers/**/*.vb"/>`
- `netstandard2.0`: `<Compile Include="Providers/NetStandard2_0/**/*.vb"/>`
- `net8.0|net10.0`: `<Compile Include="Providers/Net8_0Plus/**/*.vb"/>`

### 3.5 Office-/Archiv-Semantik (fail-closed)
- Office/OpenDocument-Endungen werden alias-basiert auf gruppierte Typen aufgeloest (`Docx`, `Xlsx`, `Pptx`).
- Legacy-OLE2 (`.doc/.xls/.ppt`) wird ueber `LegacyOfficeBinaryRefiner` markerbasiert fail-closed verfeinert.
- `TryValidateArchive` und Extraktion akzeptieren nur echte extrahierbare Archiv-Container (`Zip`); Office-Container werden nicht als extrahierbares Archiv behandelt.
- Endungspruefung bleibt nachgelagerte Policy und wird nur bei explizitem Verify-Flag als Fehlerpfad erzwungen.

## 4. Verifikation/Nachweise
### 4.1 Befehle und Exit-Codes
1. `dotnet --info` -> `0` (`artifacts/ci/netstandard2-compat/dotnet-info.txt`)
2. `dotnet restore FileClassifier.sln -v minimal` -> `0`
3. `dotnet restore --locked-mode FileClassifier.sln -v minimal` -> `0`
4. `dotnet build FileClassifier.sln -c Release --no-restore -warnaserror -v minimal` -> `0`
5. `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release --no-build -v minimal` -> `0` (`544` Tests gruen)
6. `dotnet pack src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release --no-build -o artifacts/ci/netstandard2-compat/nuget -v minimal` -> `0`
7. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f netstandard2.0 -v diag > artifacts/ci/netstandard2-compat/build-netstandard2.0.log` -> `0`
8. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f net8.0 -v diag > artifacts/ci/netstandard2-compat/build-net8.0.log` -> `0`
9. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f net10.0 -v diag > artifacts/ci/netstandard2-compat/build-net10.0.log` -> `0`
10. `python3 tools/check-doc-consistency.py` -> `0`
11. `python3 tools/check-docs.py` -> `0`
12. `EXPECTED_RELEASE_TAG=v5.2.0-rc.6 REQUIRE_RELEASE_TAG=1 bash tools/ci/bin/run.sh versioning-svt` -> `0`
13. `bash tools/ci/bin/run.sh version-convergence` -> `0`
14. `bash tools/ci/bin/run.sh security-nuget` -> `0`

### 4.2 Build-/Pack-Proof
- Build-Matrix erfolgreich:
  - `src/FileTypeDetection/bin/Release/netstandard2.0/Tomtastisch.FileClassifier.dll`
  - `src/FileTypeDetection/bin/Release/net8.0/Tomtastisch.FileClassifier.dll`
  - `src/FileTypeDetection/bin/Release/net10.0/Tomtastisch.FileClassifier.dll`
- NUPKG-Inhalt (`artifacts/ci/netstandard2-compat/nuget/Tomtastisch.FileClassifier.5.2.0.nupkg`):
  - `lib/netstandard2.0/Tomtastisch.FileClassifier.dll`
  - `lib/net8.0/Tomtastisch.FileClassifier.dll`
  - `lib/net10.0/Tomtastisch.FileClassifier.dll`

### 4.3 Provider-Compile-Proof
- Build-Task-Proof je TFM: `artifacts/ci/netstandard2-compat/provider-compile-proof-short.txt`
  - `netstandard2.0` -> `Providers/NetStandard2_0/HashPrimitivesProvider.vb`
  - `net8.0` -> `Providers/Net8_0Plus/HashPrimitivesProvider.vb`
  - `net10.0` -> `Providers/Net8_0Plus/HashPrimitivesProvider.vb`
- Runtime-Marker-Proof: `artifacts/ci/netstandard2-compat/provider-marker-proof.txt`
  - `netstandard2.0:NetStandard2_0`
  - `net8.0:Net8_0Plus`
  - `net10.0:Net8_0Plus`

### 4.4 Forbidden-API Grep-Proof (Core)
Befehl:
```bash
rg -n "Convert\.ToHexString|SHA256\.HashData|System\.IO\.Hashing|Microsoft\.AspNetCore\.App" src/FileTypeDetection/Core
```
Ergebnis:
- keine Treffer (`artifacts/ci/netstandard2-compat/core-forbidden-apis.txt` hat `0` Zeilen)

### 4.5 Test-/Semantik-Proof (Office/OpenOffice/Archive)
- Neue/erweiterte Tests decken u. a. ab:
  - falsche Endung vs. Inhaltsdetektion (`verifyExtension=false/true`)
  - Legacy-OLE Office (`doc/xls/ppt`)
  - OpenDocument (`odt/ods/odp`)
  - echte Archive vs. Office-Container
  - korrupte Payloads und Konfliktmarker (fail-closed)
- Relevante Testdateien:
  - `tests/FileTypeDetectionLib.Tests/Unit/EndToEndFailClosedMatrixUnitTests.cs`
  - `tests/FileTypeDetectionLib.Tests/Unit/LegacyOfficeBinaryRefinerUnitTests.cs`
  - `tests/FileTypeDetectionLib.Tests/Unit/OpenXmlRefinerUnitTests.cs`
  - `tests/FileTypeDetectionLib.Tests/Unit/ExtensionCheckUnitTests.cs`
  - `tests/FileTypeDetectionLib.Tests/Unit/ArchiveExtractionUnitTests.cs`

### 4.6 Version-/Release-Konvergenz
- `artifacts/versioning_report.json` -> `status: pass`, `expected_version: 5.2.0-rc.6`
- `artifacts/ci/versioning-svt/versioning-svt-summary.json` -> `status: pass`
- `artifacts/ci/version-convergence/summary.json` -> `status: pass`, `repo_version=5.2.0`, `vbproj_version=5.2.0`, `docs_latest_version=5.2.0`
- RC-PreRelease-NUPKG fuer SVT-Probe: `artifacts/nuget/Tomtastisch.FileClassifier.5.2.0-rc.6.nupkg`

### 4.7 Policy/Konvergenz-Notiz
Ambiguitaet zwischen:
- `docs/versioning/001_POLICY_VERSIONING.MD` (Tag `vX.Y.Z[-prerelease]` als SSOT fuer Publish), und
- Repo-Konvergenzregeln (`RepoVersion`/`Version`/`PackageVersion` bleiben Kernversion `X.Y.Z`).

Entscheidung fuer diesen Scope:
- fail-closed nach bestehendem CI/Repo-Vertrag: Kernversionen bleiben auf `5.2.0` konvergent.
- Pre-Release wird ueber Tag/NUPKG-Version `v5.2.0-rc.6` abgebildet und via SVT geprueft.

## 5. Grenzen/Nicht-Ziele
- Keine oeffentliche API-Signatur geaendert.
- Keine Runtime-Provider-Umschaltung eingefuehrt.
- Keine Blocker-Datei notwendig (`999_NETSTANDARD2_BLOCKERS.MD` nicht erstellt).

## 6. Verlinkte SSOT-Quellen
- [Versioning Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/001_POLICY_VERSIONING.MD)
- [Code Quality Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
- [Documentation Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/004_POLICY_DOCUMENTATION.MD)
- [Audit Index](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/000_INDEX.MD)
