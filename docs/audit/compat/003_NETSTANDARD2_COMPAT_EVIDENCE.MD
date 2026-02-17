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
3. `dotnet restore --locked-mode FileClassifier.sln -v minimal` -> `0`
4. `dotnet build FileClassifier.sln -c Release --no-restore -warnaserror -v minimal` -> `0`
5. `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release --no-build -v minimal` -> `0` (`414` Tests gruen)
6. `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release --no-build --filter "Category=ApiContract" -v minimal` -> `0` (`3` Tests gruen)
7. `dotnet pack src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release --no-build -o artifacts/ci/netstandard2-compat/nuget -v minimal` -> `0`
8. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f netstandard2.0 -v diag > artifacts/ci/netstandard2-compat/build-netstandard2.0.log` -> `0`
9. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f net8.0 -v diag > artifacts/ci/netstandard2-compat/build-net8.0.log` -> `0`
10. `dotnet build src/FileTypeDetection/FileTypeDetectionLib.vbproj -c Release -f net10.0 -v diag > artifacts/ci/netstandard2-compat/build-net10.0.log` -> `0`
11. `python3 tools/check-doc-consistency.py` -> `0`
12. `python3 tools/check-docs.py` -> `0`
13. `bash tools/ci/bin/run.sh versioning-svt` -> `0`
14. `bash tools/ci/bin/run.sh version-convergence` -> `0`
15. `bash tools/ci/bin/run.sh security-nuget` -> `0`

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
- `netstandard2.0`:
  - `artifacts/ci/netstandard2-compat/build-netstandard2.0.log:48497`
  - Treffer: `Providers/NetStandard2_0/HashPrimitivesProvider.vb (Aufgaben-ID: 7)`
- `net8.0`:
  - `artifacts/ci/netstandard2-compat/build-net8.0.log:48421`
  - Treffer: `Providers/Net8_0Plus/HashPrimitivesProvider.vb (Aufgaben-ID: 12)`
- `net10.0`:
  - `artifacts/ci/netstandard2-compat/build-net10.0.log:48416`
  - Treffer: `Providers/Net8_0Plus/HashPrimitivesProvider.vb (Aufgaben-ID: 12)`
- Negativ-Proof:
  - kein Treffer fuer `Providers/Net8_0Plus/HashPrimitivesProvider.vb (Aufgaben-ID: 7)` in `build-netstandard2.0.log`
  - kein Treffer fuer `Providers/NetStandard2_0/HashPrimitivesProvider.vb (Aufgaben-ID: 12)` in `build-net8.0.log` und `build-net10.0.log`

### 4.4 Forbidden-API Grep-Proof (Core)
Befehl:
```bash
rg -n "Convert\.ToHexString|SHA256\.HashData|System\.IO\.Hashing|Microsoft\.AspNetCore\.App" src/FileTypeDetection/Core
```
Ergebnis:
- keine Treffer, Exit-Code `1` (expected for no-match)

### 4.5 CI-Teilchecks
- `artifacts/ci/versioning-svt/versioning-svt-summary.json` -> `status: pass`
- `artifacts/ci/version-convergence/summary.json` -> `status: pass`, `repo_version=5.2.0`, `vbproj_version=5.2.0`, `docs_latest_version=5.2.0`
- `artifacts/ci/security-nuget/result.json` -> `status: pass`

### 4.6 Policy Ambiguity
Ambiguitaet zwischen:
- `docs/versioning/001_POLICY_VERSIONING.MD:43` (in PR/CI keine statischen Versionfelder), und
- existierendem SVT/Convergence-Setup (`verify-version-convergence.sh`, `check-versioning-svt.sh`), das `RepoVersion` und `Version`/`PackageVersion` in `FileTypeDetectionLib.vbproj` erwartet.

Entscheidung fuer diesen Scope:
- fail-closed nach bestehendem CI/Repo-Vertrag: Versionen auf `5.2.0` synchron gehalten und durch `versioning-svt` + `version-convergence` verifiziert.

## 5. Grenzen/Nicht-Ziele
- Keine oeffentliche API-Signatur geaendert.
- Keine Runtime-Provider-Umschaltung eingefuehrt.
- Keine Blocker-Datei notwendig (`999_NETSTANDARD2_BLOCKERS.MD` nicht erstellt).

## 6. Verlinkte SSOT-Quellen
- [Versioning Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/001_POLICY_VERSIONING.MD)
- [Code Quality Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
- [Documentation Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/004_POLICY_DOCUMENTATION.MD)
- [Audit Index](https://github.com/tomtastisch/FileClassifier/blob/main/docs/audit/000_INDEX.MD)
