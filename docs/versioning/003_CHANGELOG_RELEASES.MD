# Changelog

Alle Aenderungen werden hier technisch dokumentiert. Die Release-Version selbst ist
der Git-Tag `vX.Y.Z` (optional `-prerelease`) als SSOT.

## [Unreleased]
- Added:
- Changed:
- Fixed:
- Docs/CI/Tooling:

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
