# 045 Compliance Status (DE)

Stand: 2026-02-20  
Geltungsbereich: `src/FileTypeDetection/*`

## Ziel
Nachweisbare, reproduzierbare Einhaltung der Kernvorgaben aus `045_CODE_QUALITY_POLICY_DE.MD` über automatisierte Tests und wiederholbare Build-/Testläufe.

## Automatisierte Governance-Gates
1. Public-API-Allowlist-Gate  
   Test: `PublicApiAllowlistTests`  
   Datei: `tests/FileTypeDetectionLib.Tests/Contracts/PublicApiAllowlistTests.cs`  
   Wirkung: Unerwartete neue Public-Typen werden als blocker erkannt.

2. Public-API-Snapshot-Gate  
   Test: `PublicApiContractSnapshotTests`  
   Datei: `tests/FileTypeDetectionLib.Tests/Contracts/PublicApiContractSnapshotTests.cs`  
   Wirkung: Surface-Drift in Public API wird deterministisch sichtbar.

3. 045 Core-Layout-/Policy-Gate  
   Test: `CodePolicy045ComplianceTests`  
   Datei: `tests/FileTypeDetectionLib.Tests/Contracts/CodePolicy045ComplianceTests.cs`  
   Geprüfte Regeln:
   - Header enthält `' FILE:` und `INTERNE POLICY`
   - `Option Strict On` und `Option Explicit On` vorhanden
   - Reihenfolge: Header -> Options -> Namespace
   - Verbotener Catch-Pseudofilter wird ausgeschlossen
   - Dateien mit `Public`-Typen enthalten XML-`summary`
   - Scan nur auf Source-Dateien (Ausschluss `bin/` und `obj/`)

## Reproduzierbare Verifikation
1. `dotnet build FileClassifier.sln -v minimal`
2. `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal`

## Iterative Verbesserungen (Issue 03)
1. Interne Duplikatkonsolidierung:
   - `FileTypeDetector` nutzt zentrale Registry-Regel für structured refinement.
   - Alias-Definitionen in `FileTypeRegistry` über Helper vereinheitlicht.
2. Policy-Header harmonisiert:
   - `HashRoundTripReport.vb`, `EnumUtils.vb`, `GuardUtils.vb`, `IterableUtils.vb`.

## Hinweis
Diese Statusdatei dokumentiert den nachweisbaren Kernumfang der automatisierten 045-Compliance-Prüfung.  
Erweiterungen an den Governance-Gates sind möglich, müssen jedoch fail-closed und mit niedriger False-Positive-Rate erfolgen.
