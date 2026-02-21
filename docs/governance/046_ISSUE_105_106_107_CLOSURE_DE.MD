# Issue Closure Matrix (DE) - #105 / #106 / #107

Stand: 2026-02-21

## Ziel
Nachweis, dass alle offenen Punkte aus den Issues `#105`, `#106` und `#107` im PR `#108` umgesetzt sind.

## Issue #105 - HashRoundTripReport ersetzen und Usages migrieren
Status: umgesetzt.

Abdeckung:
- Datei ersetzt: `src/FileTypeDetection/Abstractions/Hashing/HashRoundTripReport.vb`
- Legacy-Usages migriert auf neue API (`HashSlot`, `Evidence(slot)`, `LogicalEquals(slot)`, `PhysicalEquals(slot)`).
- API-Contract/Snapshot aktualisiert:
  - `tests/FileTypeDetectionLib.Tests/Contracts/public-api.snapshot.txt`

Geforderte `rg`-Trefferliste alt vs. neu:
1. Legacy-API-Symbole (alt)
   - Kommando:
     - `rg -n "LogicalH1Equals|PhysicalH1Equals|HashRoundTripReport\\.(H1|H2|H3|H4)\\b" src tests -S`
   - Ergebnis:
     - `0` Treffer
2. Neue API-Symbole (neu)
   - Kommando:
     - `rg -n "HashSlot|Evidence\\(|LogicalEquals\\(|PhysicalEquals\\(" src tests -S`
   - Ergebnis:
     - Treffer in `HashRoundTripReport.vb`, `HashingEvidenceTests.cs`, `FileTypeDetectionSteps.cs` und `public-api.snapshot.txt`.

Build/Test-Evidence:
- `dotnet build FileClassifier.sln -v minimal` -> pass
- `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal` -> pass

## Issue #106 - Public-API-Policy haerten (Allowlist + Contract-Gates)
Status: umgesetzt.

Abdeckung:
- Explizite Public-API-Allowlist als blocker:
  - `tests/FileTypeDetectionLib.Tests/Contracts/PublicApiAllowlistTests.cs`
- Public-API-Snapshot-Gate als blocker:
  - `tests/FileTypeDetectionLib.Tests/Contracts/PublicApiContractSnapshotTests.cs`
- Policy-Compliance-Gate als blocker:
  - `tests/FileTypeDetectionLib.Tests/Contracts/CodePolicy045ComplianceTests.cs`

Dokumentierte freigegebene Public-Hauptklassen:
- In `AllowedPublicTypes` innerhalb
  `tests/FileTypeDetectionLib.Tests/Contracts/PublicApiAllowlistTests.cs`.

Reproduzierbarer Governance-Check:
- `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal`

## Issue #107 - Interne Refactor-/Utility-Konsolidierung
Status: umgesetzt.

Refactor-Matrix (Duplikatquelle -> neue Utility-Stelle):
1. Hashing-Kernlogik in Fassade `EvidenceHashing.vb`
   - -> `EvidenceHashingCore.vb`
   - Methoden u. a.: `BuildEvidenceFromEntries`, `BuildEvidenceFromRawPayload`,
     `TryNormalizeEntries`, `BuildLogicalManifestBytes`, `ComputeSha256Hex`,
     `ComputeFastHash`, `ComputeHmacSha256Hex`.
2. RoundTrip-I/O/Temp-Lifecycle in Fassade
   - -> `EvidenceHashingRoundTrip.vb`
   - Pipeline: TempRoot/Target, Materialisierung, Cleanup, Report-Bildung.
3. Gebundene Datei-Leselogik/Fehleraufbereitung
   - -> `EvidenceHashingIo.vb`
   - Methoden: `TryReadFileBounded`, `SetReadFileError`.
4. Enum- und Guard-Helfer in mehreren Klassen
   - -> `EnumUtils.vb`, `GuardUtils.vb`, `IterableUtils.vb`
   - Einsatz sichtbar in `HashRoundTripReport.vb`.

Evidence-Kommandos:
- `rg -n "EvidenceHashingCore|EvidenceHashingRoundTrip|EvidenceHashingIo" src tests -S`
- `rg -n "EnumUtils\\.|GuardUtils\\.|IterableUtils\\." src tests -S`
- `dotnet test FileClassifier.sln -v minimal`

## Entscheidung
Alle offenen Punkte aus `#105`, `#106` und `#107` sind im aktuellen PR-Stand umgesetzt und auditierbar nachgewiesen.
