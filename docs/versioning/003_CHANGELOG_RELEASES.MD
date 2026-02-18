<!-- LANG_SWITCH:BEGIN -->
[DE](003_CHANGELOG_RELEASES.MD) | [EN](103_CHANGELOG_RELEASES.MD)
<!-- LANG_SWITCH:END -->

# Changelog

Alle Aenderungen werden hier technisch dokumentiert. Die Release-Version selbst ist
der Git-Tag `vX.Y.Z` (optional `-prerelease`) als SSOT.

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
  - Retention-Workflow auf OIDC-Key-Fallback (`NuGet/login`) fuer NuGet-Unlist umgestellt.

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
