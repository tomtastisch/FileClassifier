# Inventory: netstandard2 Compat Baseline

## 1. Zweck
Dieses Dokument protokolliert den Ausgangs-/Zwischenstand der TFM- und API-sensitiven Fundstellen fuer den netstandard2.0-Refactor.

## 2. Geltungsbereich
- Library-Projekt: `src/FileTypeDetection/FileTypeDetectionLib.vbproj`
- Produktionscode: `src/FileTypeDetection/**/*.vb`
- Tests: `tests/FileTypeDetectionLib.Tests/**/*`

## 3. Regeln/Architektur
Erfasste Suchmuster:
- `Convert.ToHexString`
- `SHA256.HashData`
- `System.IO.Hashing`
- `XxHash3`
- `FrameworkReference Microsoft.AspNetCore.App`
- `TargetFrameworks`

## 4. Verifikation/Nachweise
### 4.1 Projekt/TFMs
Befehl:
```bash
rg -n "TargetFrameworks|FrameworkReference|PackageReference Include=\"System\.IO\.Hashing\"|PackageReference Include=\"Microsoft\.Extensions\.Logging\.Abstractions\"" src/FileTypeDetection/FileTypeDetectionLib.vbproj
```
Ausgabe:
- `src/FileTypeDetection/FileTypeDetectionLib.vbproj:6` -> `TargetFrameworks netstandard2.0;net8.0;net10.0`
- `src/FileTypeDetection/FileTypeDetectionLib.vbproj:26` -> `Microsoft.Extensions.Logging.Abstractions`
- `src/FileTypeDetection/FileTypeDetectionLib.vbproj:29` -> `System.IO.Hashing`

### 4.2 Produktionscode
Befehl:
```bash
rg -n "Convert\.ToHexString|SHA256\.HashData|System\.IO\.Hashing|XxHash3" src/FileTypeDetection --glob '*.vb' --glob '*.vbproj'
```
Relevante Treffer:
- `src/FileTypeDetection/Providers/Net8_0Plus/HashPrimitivesProvider.vb:49` (`Convert.ToHexString`)
- `src/FileTypeDetection/Providers/Net8_0Plus/HashPrimitivesProvider.vb:64` (`SHA256.HashData`)
- `src/FileTypeDetection/Providers/Net8_0Plus/HashPrimitivesProvider.vb:77` (`XxHash3`)
- `src/FileTypeDetection/Providers/NetStandard2_0/HashPrimitivesProvider.vb:91` (`XxHash3`)

### 4.3 AspNetCore FrameworkReference
Befehl:
```bash
rg -n "Microsoft\.AspNetCore\.App|AspNetCore" src/FileTypeDetection --glob '*.vb' --glob '*.vbproj'
```
Ausgabe:
- Keine Treffer (exit code 1) -> FrameworkReference entfernt.

### 4.4 Tests
Befehl:
```bash
rg -n "Convert\.ToHexString|SHA256\.HashData|System\.IO\.Hashing|XxHash3" tests/FileTypeDetectionLib.Tests --glob '*.cs' --glob '*.txt'
```
Relevante Treffer (expected in tests):
- `tests/FileTypeDetectionLib.Tests/Support/FixtureManifestCatalog.cs:133-134`
- `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs:2,45,61,349,372-373`
- `tests/FileTypeDetectionLib.Tests/Contracts/public-api.snapshot.txt:75-76`

## 5. Grenzen/Nicht-Ziele
- Dieses Inventar ist ein technischer Suchlauf und ersetzt keine Laufzeit- oder CI-Verifikation.
- Nicht gelistet sind reine Dokumenttexte ausserhalb des Produktions-/Testcodes.

## 6. Verlinkte SSOT-Quellen
- [Versioning Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/001_POLICY_VERSIONING.MD)
- [Code Quality Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
