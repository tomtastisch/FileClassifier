<!-- LANG_SWITCH:BEGIN -->
[DE](003_CHANGELOG_RELEASES.MD) | [EN](103_CHANGELOG_RELEASES.MD)
<!-- LANG_SWITCH:END -->

# Changelog

Alle Änderungen werden hier technisch dokumentiert. Die Release-Version selbst ist
der Git-Tag `vX.Y.Z` (optional `-prerelease`) als SSOT.

## [6.1.12]
- Added:
  - Kanonische Rasterstruktur fuer die zweisprachige Dokumentation in `docs/0_de` und `docs/1_en` mit identischer Dateimenge pro Sprache eingefuehrt.
  - Sync-Tooling `tools/docs/sync_bilingual_structure.py` um fail-closed-Pruefungen fuer Language-Switch-Header erweitert.
- Changed:
  - EN-Versionierungsdokumentation (`102/103`) auf den aktuellen 6.x-Stand harmonisiert.
  - Sync-Tooling entfernt stale Ziel-Dateien deterministisch (Prune), validiert Dateiparitaet und erzwingt Umschaltbloecke in beiden Sprachraeumen.
  - PR-Scope-Allowlist auf neue Rasterpfade erweitert (`docs/0_de/**`, `docs/1_en/**`), inklusive Lang-Switch-Report-Dateien.
- Docs/CI/Tooling:
  - Vollstaendiger lokaler Required-Checks-Lauf (`preflight` bis `summary`) erfolgreich als End-to-End-Evidence ausgefuehrt.
  - Versionskonvergenz aktiv auf `6.1.12` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.11]
- Added:
  - Neue CSCore-Utility `ArchivePathPolicyUtility` fuer relative Archivpfad-Normalisierung und Root-Path-Policy.
  - Neue Bridge-Operationen `NormalizeArchiveRelativePath` und `IsRootPath`.
- Changed:
  - `ArchiveEntryPathPolicy.TryNormalizeRelativePath` delegiert bevorzugt an CSCore und faellt fail-closed auf VB-Fallback zurueck.
  - `DefaultDestinationPathPolicy.IsRootPath` delegiert bevorzugt an CSCore und faellt fail-closed auf VB-Fallback zurueck.
  - `CsCoreUtilityBridgeUnitTests` erweitern die Telemetrieabdeckung um Archive-/Path-Policy-Delegation.
  - Package-backed Contract prueft zusaetzlich die Presence von `ArchivePathPolicyUtility`.
  - CSCore-Utilities-README und Referenzdokumente (DE/EN) um den Archive-/Path-Policy-Pfad erweitert.
- Docs/CI/Tooling:
  - Versionskonvergenz aktiv auf `6.1.11` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.10]
- Added:
  - Neue CSCore-Utility `EvidencePolicyUtility` fuer Hash-Evidence-Policy (Label-Normalisierung, Note-Append, HMAC-Env-Key-Resolution).
  - Neue Bridge-Operationen `NormalizeEvidenceLabel`, `AppendNoteIfAny` und `ResolveHmacKeyFromEnvironment`.
- Changed:
  - `EvidenceHashingCore.NormalizeLabel`, `AppendNoteIfAny` und `TryResolveHmacKey` delegieren bevorzugt an CSCore und behalten fail-closed VB-Fallback bei.
  - `CsCoreUtilityBridgeUnitTests` erweitern die Telemetrie- und Delegationsabdeckung fuer die neuen Evidence-Policy-Pfade.
  - Package-backed Contract prueft zusaetzlich die Presence von `EvidencePolicyUtility`.
  - CSCore-Utilities-README und Referenzdokumente (DE/EN) um den Evidence-Policy-Pfad erweitert.
- Docs/CI/Tooling:
  - Versionskonvergenz aktiv auf `6.1.10` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.9]
- Added:
  - Neue CSCore-Utility `OfficePolicyUtility` fuer OpenXML/OpenDocument- und Legacy-Office-Entscheidungslogik.
  - Bridge-Operationen `ResolveOpenDocumentMimeKindKey`, `ResolveArchivePackageKindKey` und `ResolveLegacyMarkerKindKey`.
- Changed:
  - `OpenXmlRefiner` delegiert MIME- und Paketentscheidungen bevorzugt an CSCore und faellt fail-closed auf VB-Logik zurueck.
  - `LegacyOfficeBinaryRefiner` delegiert Marker-Entscheidungen bevorzugt an CSCore und faellt fail-closed auf VB-Logik zurueck.
  - `CsCoreUtilityBridgeUnitTests` pruefen zusaetzlich die neuen Office-Policy-Delegationspfade inkl. Telemetriezaehlung.
  - CSCore-Utilities-README und Referenzdokumente (DE/EN) um Office-Policy-Pfad erweitert.
- Docs/CI/Tooling:
  - Versionskonvergenz aktiv auf `6.1.9` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.8]
- Added:
  - Neue CSCore-Utility `DetectionPolicyUtility` fuer Endungs-Policy, Reason-Code-Mapping und Detection-Summary-Projektion.
  - Vollstaendige CSCore-Readme-Abdeckung fuer Root, `Model`, `Mapping` und `Utilities`.
  - Neue Bridge-Operationen `BuildSummaryValues`, `IsExtensionMatch`, `ExceptionToReasonCode` inkl. Telemetriezaehlung.
- Changed:
  - `FileTypeDetector.DetectDetailed` und `DetectAndVerifyExtension` nutzen jetzt projektierten CSCore-Summary-Kontext plus fail-closed VB-Fallback.
  - `FileTypeDetector.ExceptionToReasonCode` delegiert bevorzugt an CSCore und faellt deterministisch auf VB-Mapping zurueck.
  - `CsCoreUtilityBridgeUnitTests` pruefen zusaetzlich die neuen Delegationspfade (`BuildSummaryValues`, `IsExtensionMatch`, `ExceptionToReasonCode`).
  - In-Code-Dokumentation im CSCore systematisch nachgezogen (XML-Kommentare fuer Modelle, Mapper und Utilities).
- Docs/CI/Tooling:
  - Referenzdokumente DE/EN um CSCore-Bridge/Policy-Pfade und compile-time Abhaengigkeiten (`Riok.Mapperly`, `PolySharp`) erweitert.
  - Versionskonvergenz aktiv auf `6.1.8` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.7]
- Added:
  - Dependency-Canary um `System.Text.Encoding.CodePages` erweitert (Risk-Scope: `archive-text-encoding`).
  - BDD-Step-Alias `JPG -> Jpeg` fuer stabile Legacy-Feature-Kompatibilitaet ergaenzt.
- Changed:
  - Analyzer-Major-Upgrade: `Meziantou.Analyzer` auf `3.0.12`, `Microsoft.CodeAnalysis.NetAnalyzers` auf `10.0.103`.
  - `System.Text.Encoding.CodePages` als direkte, zentral gepinnte Abhaengigkeit (`10.0.3`) fuer `netstandard2.0` aufgenommen.
  - `CsCoreRuntimeBridge` fuer Coverage-Metrik explizit ausgenommen, damit BDD-Coverage-Gate weiterhin den fachlichen Produktivcode statt Reflection-Bridge-Plumbing bewertet.
  - Versionshistorien-Links fuer lokale `6.x`-Unreleased-Eintraege auf stabilen `compare/main...HEAD`-Anker vereinheitlicht (Docs-Link-Gate fail-closed).
- Docs/CI/Tooling:
  - Supply-Chain-Baseline (DE/EN) um verbindliche netstandard2.0-Dependency-Strategie erweitert (CodePages-Pin + Microsoft.NETCore.Platforms-Meta-Ausnahmefall mit Security-Gate).
  - Vollstaendiger lokaler run.sh-End-to-End-Lauf (`preflight` bis `summary`) erfolgreich.
  - Versionskonvergenz aktiv auf `6.1.7` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.6]
- Added:
  - Paket `PolySharp` im CSCore fuer compile-time Erzeugung des `IsExternalInit`-Polyfills (Record-/Init-Compat ohne manuelle Polyfill-Datei).
  - Fail-closed Banned-API-Banlist um unsichere direkte ZIP-Extraktions-APIs erweitert (`ZipFile.ExtractToDirectory*`, `ZipFileExtensions.ExtractToFile*`).
- Changed:
  - `DotNet.ReproducibleBuilds` auf `2.0.2` angehoben.
  - `FileTypeDetectionLib` erzwingt jetzt ebenfalls `Microsoft.CodeAnalysis.BannedApiAnalyzers` mit zentraler Banlist (`tools/analyzers/BannedSymbols.txt`).
  - `CsCoreRuntimeBridge` laedt CSCore nicht mehr per `Assembly.LoadFrom`, sondern ueber kontrollierten Byte-Load-Fallback (`Assembly.Load(byte[])`) mit fail-closed Exception-Filter.
  - Manuelle Datei `src/FileClassifier.CSCore/Compatibility/IsExternalInit.cs` entfällt zugunsten von compile-time Codegen.
- Docs/CI/Tooling:
  - Cross-Language-Governance-Regeln (DE/EN) fuer Polyfill-Quelle und Banned-API-Mindestanforderung auf `FileTypeDetectionLib` aktualisiert.
  - Versionskonvergenz aktiv auf `6.1.6` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.5]
- Added:
  - Paket `DotNet.ReproducibleBuilds` als zentrale Build-Abhaengigkeit fuer alle internen Projekte (ausgenommen `PortableConsumer`) zur Staerkung von Reproduzierbarkeit/Provenance.
- Changed:
  - Reproducible-Build-Baseline wird jetzt systemweit ueber `Directory.Build.props` ausgerollt statt projektweise.
- Docs/CI/Tooling:
  - Versionskonvergenz aktiv auf `6.1.5` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.4]
- Added:
  - Neues CSCore-Optionsmodell auf Record-Basis (`ProjectOptionsSnapshot`, `HashOptionsSnapshot`) inklusive compile-time Mapperly-Mapper (`ProjectOptionsSnapshotMapper`).
  - Neue CSCore-Utility `ProjectOptionsUtility` zur zentralen, deterministischen Normalisierung von Projekt-/Hash-Optionen.
  - Gemeinsame Analyzer-Banlist fuer C#-Projekte unter `tools/analyzers/BannedSymbols.txt`.
- Changed:
  - `FileTypeProjectOptions.NormalizeInPlace` delegiert die Werte-Normalisierung bevorzugt an CSCore (`TryNormalizeProjectOptionsValues`) und faellt fail-closed auf VB-Fallback zurueck.
  - `CsCoreRuntimeBridge` um Operation `NormalizeProjectOptionsValues` erweitert (inkl. Telemetry delegated/fallback).
  - C#-Analyzer-Baseline (`NetAnalyzers`, `BannedApiAnalyzers`, `Meziantou`) zentral auf alle relevanten C#-Projekte ausgerollt.
  - CSCore verwendet die zentrale Banned-API-Datei statt projektspezifischer Duplikate.
  - Package-backed Contract und Bridge-Tests pruefen zusaetzlich `ProjectOptionsUtility`.
- Docs/CI/Tooling:
  - Versionskonvergenz aktiv auf `6.1.4` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.3]
- Added:
  - Neue CSCore-Utilities fuer Hash-/Materializer-Entscheidungen (`HashNormalizationUtility`, `MaterializationUtility`).
  - Bridge-Telemetry mit Snapshot-API (`ResetTelemetry`, `GetTelemetrySnapshot`) fuer auditierbaren Delegation-vs-Fallback-Nachweis.
  - Zusätzliche Regressionen:
    - deterministic decision-matrix property test fuer `FileMaterializer`-SecureExtract-Flow,
    - Bridge-Telemetry-Test fuer Hashing-/Materializer-Pfade.
- Changed:
  - `HashDigestSet` delegiert Normalisierung und Empty-Part-Erzeugung bevorzugt an CSCore.
  - `HashOptions` delegiert Coalesce-/Filename-Normalisierung bevorzugt an CSCore.
  - `FileMaterializer` delegiert Payload-Limit- und Materialisierungsmodus-Entscheidung bevorzugt an CSCore; Fallback bleibt fail-closed in VB.
  - `FileMaterializer` haertet den Delegationspfad zusaetzlich: CSCore-Modus wird gegen lokale fail-closed Entscheidungslogik verifiziert (Mode-Mismatch => lokaler deterministischer Fallback).
  - `CsCoreRuntimeBridge.GetTelemetrySnapshot()` initialisiert die Bridge explizit vor Snapshot-Bildung, damit `IsCsCoreAvailable` auch ohne Voraufruf auditierbar korrekt ist.
  - Package-backed Contract prueft zusaetzlich Presence der neuen CSCore-Utility-Typen.
- Docs/CI/Tooling:
  - Versionskonvergenz aktiv auf `6.1.3` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.2]
- Added:
  - Neue CSCore-Utility-Schicht fuer `EnumUtility`, `IterableUtility`, `GuardUtility` und `ExceptionFilterUtility`.
  - VB-Runtime-Bridge `CsCoreRuntimeBridge` als fail-closed Delegationspunkt (mit deterministischem Fallback auf bestehende VB-Logik).
  - Neue Unit-Tests `CsCoreUtilityBridgeUnitTests` zur Paritaets-/Fallback-Absicherung.
- Changed:
  - `EnumUtils`, `IterableUtils`, `ArgumentGuard` und `ExceptionFilterGuard` delegieren bevorzugt auf CSCore (wenn verfuegbar).
  - Package-backed Contract prueft zusaetzlich die Presence der neuen CSCore-Utility-Typen.
  - Build-Orchestrierung erweitert: CSCore-Buildartefakte werden beim lokalen Build auch in `$(TargetDir)` kopiert.
- Docs/CI/Tooling:
  - Versionskonvergenz aktiv auf `6.1.2` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.1.1]
- Added:
  - Neues C#-Teilprojekt `src/FileClassifier.CSCore` mit `record`-basierten Datenmodellen (`DetectionSignal`, `DetectionSummary`) und Mapperly-Mapper.
  - Fail-closed API-Verbotsliste via `BannedSymbols.txt` + `Microsoft.CodeAnalysis.BannedApiAnalyzers`.
- Changed:
  - Single-Package-Orchestrierung: `FileClassifier.CSCore.dll` wird pro TFM deterministisch gebaut und in das Hauptpaket eingebettet.
  - CI-Pack-Gate haertet den Contract explizit: fehlende `FileClassifier.CSCore.dll` im nupkg => `fail`.
  - Versionierungsfortschritt fuer die Issue-Kette explizit dokumentiert: `6.0.1` (FC-0001) -> `6.0.2` (FC-0002) -> `6.1.0` (FC-0003) -> `6.1.1` (FC-0004).
- Fixed:
  - `dotnet format ... analyzers` Regression durch Solution-Einbindung von CSCore beseitigt (CSCore aus `.sln` entfernt, Build stattdessen ueber MSBuild-Targets im VB-Projekt orchestriert).
- Docs/CI/Tooling:
  - Governance-Regeln fuer Cross-Language-CSCore im DE-Policy-Set dokumentiert.
  - Versionskonvergenz aktiv auf `6.1.1` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [6.0.0]
- Added:
  - Neue interne Hashing-Services (`EvidenceHashingCore`, `EvidenceHashingRoundTrip`, `EvidenceHashingIO`) für deterministische Auslagerung ohne neue Dependencies.
- Changed:
  - Breaking-Umstellung von `FileKind`-Werten (`Docx`/`Xlsx`/`Pptx` -> `Doc`/`Xls`/`Ppt`).
  - Breaking-Umstellung der `HashRoundTripReport`-Public-API auf Slot-basiertes Zugriffsmodell (`HashSlot`, `Evidence(...)`, `LogicalEquals(...)`, `PhysicalEquals(...)`).
  - `EvidenceHashing` auf Fassade reduziert; Kernlogik in interne stateless Services verschoben.
- Fixed:
  - Reflection-basierte Unit-Tests auf neue interne Hashing-Typen angepasst.
- Docs/CI/Tooling:
  - Versionskonvergenz aktiv auf `6.0.0` gesetzt (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [5.2.1]
- Added:
  - CI-Dokumentation um expliziten Pflicht-Gate-Charakter fuer Qodana erweitert.
- Changed:
  - SharpCompress-Aufrufe in `ArchiveInternals.vb` auf typsichere APIs (`ArchiveFactory.OpenArchive`, `GZipArchive.OpenArchive`) umgestellt.
  - `qodana.yml` fail-closed gehaertet: Job-Bypass entfernt und `QODANA_TOKEN` als harte Voraussetzung im CI-Job erzwungen.
- Fixed:
  - `tar.gz`-Verarbeitung fail-closed stabilisiert (GZip-Mismatch/Nested-Faelle konsistent behandelt).
- Docs/CI/Tooling:
  - Versionskonvergenz auf `5.2.1` nachgezogen (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie DE/EN).

## [5.2.0]
- Added:
  - Incode-Dokumentation fuer die TFM-Providermethoden komplettiert (`HashPrimitivesProvider` fuer `netstandard2.0` und `net8.0+`).
  - Legacy-Office-Refinement (`LegacyOfficeBinaryRefiner`) fuer OLE2-Dokumente mit fail-closed Marker-Logik eingefuehrt.
  - Erweiterte E2E-Matrix-Tests fuer falsche Endungen, korrupte Payloads und Office/OpenDocument-Varianten ergänzt.
- Changed:
  - Release-Linie von `v5.2.0-rc.6` auf stabilen Tag `v5.2.0` gehoben.
  - Public XML-Dokumentation auf Policy-045 ausgerichtet: unzulaessige `<exception>`-Tags in fail-closed APIs entfernt.
  - Deutsche Log-/Dokumentationstexte mit korrekten Umlauten harmonisiert.
  - Office-/OpenOffice-Aliasauflösung im `FileTypeRegistry` konsolidiert (`doc/docx/odt`, `xls/xlsx/ods`, `ppt/pptx/odp`).
  - Archivextraktion nimmt nur noch echte, extrahierbare Archive (`Zip`) an; Office-Container werden nicht mehr als extrahierbares Archiv behandelt.
  - `TryValidateArchive` prueft vor Safety-Gate explizit den erkannten Container-Typ.
  - Gate 4 (`tools/ci/release/gate4_verify_postpublish.sh`) fuer Pre-Release-Tags robust gemacht:
    - laengeres Retry-Fenster,
    - `registration` standardmaessig entkoppelt bei `vX.Y.Z-<label>`,
    - weiterhin fail-closed auf `flatcontainer`.
  - Release-Retention (`tools/ci/release/retention_apply.sh`) so angepasst, dass nach vorhandenem korrespondierendem Stable-Tag `vX.Y.Z` die RCs derselben Basisversion bereinigt werden (konkret: Promotion `v5.2.0-rc.6` -> `v5.2.0`), waehrend ein RC ohne korrespondierenden Stable-Tag erhalten bleibt.
- Fixed:
  - Release-Blocker beseitigt, bei dem Pre-Release-Publish trotz erfolgreichem Push an verzögerter Registration-Sichtbarkeit scheiterte.
- Docs/CI/Tooling:
  - NuGet-Trusted-Publishing-Doku (DE/EN) und Root-README auf den angepassten Gate-4-Ablauf aktualisiert.
  - Commit-Referenz fuer Version 5.2.0 in Versionshistorie (DE/EN) hinzugefuegt.
  - Retention-Workflow auf Secret-basiertes NuGet-Unlist (`NUGET_API_KEY`) gehaertet; OIDC bleibt auf `release.yml` beschraenkt.

## [5.1.4]
- Changed:
  - Cluster 7C: ReasonCodes/Exceptions konsolidiert und semantisch vereinheitlicht (DetectDetailed).
  - Archive-Infrastructure: redundante `System.IO.*`-Qualifizierer bereinigt (Imports + unqualifiziert).
- Fixed:
  - Qodana Findings: `ConvertToConstant.Local` und `UseUtf8StringLiteral` in Tests beseitigt.
- Docs/CI/Tooling:
  - Code-Scanning-Zero-Gate fuer Qodana-Fix-PRs entkoppelt (PRs mit Label `area:qodana` pruefen PR-ref statt `main`).

## [5.1.3]
- Added:
  - Verbindliches PR-Template in deutscher Sprache mit Checklisten, Nachbesserungs-Tracking und DoD-Matrix.
  - Governance-Policy fuer Branch-/PR-Naming (`docs/governance/007_POLICY_BRANCH_PR_NAMING_DE.MD`).
  - FsCheck-basierter Fuzzing-Smoke-Test als nachweisbarer Fuzzing-Signaltest.
- Changed:
  - `preflight` erzwingt jetzt fail-closed:
    - PR-Governance-Check (Branch/Titel/PR-Body),
    - `security/code-scanning/tools` muss 0 offene Alerts liefern.
  - Scorecard-Workflow nutzt `SECURITY_CLAIMS_TOKEN` als PAT-basierte Tokenquelle fuer robuste Governance-Auswertung.
  - Agent-Regelwerk (`AGENTS.md`) auf verbindliche iterative Abarbeitung und Merge-Gates erweitert.
- Fixed:
  - Versionskonvergenz auf `5.1.3` nachgezogen (`RepoVersion`, `Version`, `PackageVersion`, Versionshistorie).
- Docs/CI/Tooling:
  - Governance-Index um Branch-/PR-Naming-Policy erweitert.

## [5.1.2]
- Changed:
  - Gate4 Pollingdauer fuer Release-Verifikation optimiert.
- Fixed:
  - Release-/SVT-Ablauf fuer `release_tag` deterministisch stabilisiert.

## [5.1.1]
- Added:
  - Dependabot-Schutz fuer secret-pflichtige Workflows (`qodana`, `security-claims-evidence`) bei Dependabot-PR-Kontext.
- Changed:
  - Dependabot auf security-only fuer geplante Version-Bumps gestellt (`open-pull-requests-limit: 0` je Ecosystem).
  - `security-claims-evidence` auf explizites fail-closed Secret-Assert (`SECURITY_CLAIMS_TOKEN`) gehaertet.
- Fixed:
  - Secret-bedingte CI-Fehler auf Dependabot-PRs fuer `qodana` / `security-claims-evidence` beseitigt.
- Docs/CI/Tooling:
  - Versionshistorie semver-absteigend sortiert und veraltete Release-Referenzen korrigiert.

## [5.0.0]
- Added:
  - Optionale HMAC-SHA256-Digests fuer physische und logische Evidence-Hashes (`HmacPhysicalSha256`, `HmacLogicalSha256`) inkl. ENV-Key-Resolver (`FILECLASSIFIER_HMAC_KEY_B64`).
- Changed:
  - Breaking Rename der oeffentlichen Hashing-Fassade von `DeterministicHashing` auf `EvidenceHashing`.
  - Breaking Rename der oeffentlichen Hashing-Modelle auf `HashEvidence`, `HashDigestSet`, `HashOptions`, `HashRoundTripReport`, `HashSourceType`.
  - Hashing-Tests konsolidiert in eine zentrale Datei `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs`.
- Fixed:
  - xxHash3-Formatierung und Optionen deterministisch vereinheitlicht (lowercase, `x16`, leere Fast-Hashes bei deaktivierter Option).
- Docs/CI/Tooling:
  - Migrationsdoku fuer Hashing-Renames und HMAC-Key-Setup ergänzt.

## [4.4.0]
- Added:
  - Neue Governance-Policies fuer einheitliches Naming und SVT-Versioning (`CI-NAMING-001`, `CI-VERSION-001`).
- Changed:
  - Kanonischer Paket-/Assembly-/Namespace-Name auf `Tomtastisch.FileClassifier` vereinheitlicht.
  - Release-Publish auf NuGet Trusted Publishing (OIDC via `NuGet/login@v1`) umgestellt, fail-closed abgesichert.
- Fixed:
  - PR-Labeling robust gegen paginierte GitHub-File-Listen gemacht (deterministische JSON-Bildung).
  - Release-Workflow YAML-Syntax fuer Fail-Closed-Assert korrigiert.
- Docs/CI/Tooling:
  - CI um dedizierte Checks `naming-snt` und `versioning-svt` inkl. Artefakt-Nachweisen erweitert.
  - Migrationsdoku fuer Legacy-NuGet-ID (`Tomtastisch.FileTypeDetection`) ergänzt.

## [4.2.1]
- Added:
- Changed:
  - Quality-Gates auf explizites `Release`-Build/Test/Coverage gehaertet.
  - Versioning-Guard erzwingt nun einen vorhandenen History-Eintrag fuer die aktive Version.
- Fixed:
  - Reqnroll/xUnit Release-Regeneration gegen stale `obj/Release` Artefakte stabilisiert.
- Docs/CI/Tooling:
  - Versionshistorie `docs/versioning/002_HISTORY_VERSIONS.MD` fuer fehlende `4.x`-Eintraege nachgezogen.
  - Versionierungs-Policy um den Pflicht-Nachweis im Versionsverlauf erweitert.

## [4.1.2]
- Added:
- Changed:
  - Test-Toolchain auf xUnit v3 + `Reqnroll.xunit.v3` migriert.
  - BDD-Discovery/Runner-Ausführung für xUnit v3 stabilisiert (Scenario-Parity erhalten).
- Fixed:
- Docs/CI/Tooling:
  - CI in getrennte, auditierbare Jobs refaktoriert (Preflight, Build, Security, Tests+Coverage, Summary).
  - BDD-Readable Testlauf liefert TRX + lesbare Ausgabe + Coverage + Gate im Single-Run.
  - Dokumentation fuer CI-Nachweise unter `docs/ci/001_PIPELINE_CI.MD` ergaenzt.
  - Migrations-DoD und Fortschrittstracking in der PR-Beschreibung gepflegt.

## [3.0.0]
- Breaking: Rename `FileTypeDetectorOptions` -> `FileTypeProjectOptions`
- Breaking: Rename `FileTypeSecurityBaseline` -> `FileTypeProjectBaseline`

## [2.0.0]
- Breaking: Rename `ZipProcessing` -> `ArchiveProcessing`

## [1.0.0]
- Initial implementation
