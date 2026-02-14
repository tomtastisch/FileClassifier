# Changelog

Alle Aenderungen werden hier technisch dokumentiert. Die Release-Version selbst ist
der Git-Tag `vX.Y.Z` (optional `-prerelease`) als SSOT.

## [Unreleased]
- Changed:
  - NuGet Post-Publish SVT (`Gate 4`) auf adaptiven Retry-Schedule umgestellt, um die durchschnittliche Laufzeit bei unverändert fail-closed Verhalten zu senken.
  - Repo-/Package-Version auf `5.1.2` (Patch `Z + 1`) angehoben.
- Docs/CI/Tooling:
  - Retry-Schedule-Defaults und neues ENV `SVT_POSTPUBLISH_RETRY_SCHEDULE_SECONDS` in `docs/021_USAGE_NUGET.MD` und `docs/ci/002_NUGET_TRUSTED_PUBLISHING.MD` nachgezogen.

## [5.1.1]
- Added:
  - Verbindliches PR-Template in deutscher Sprache mit Checklisten, Nachbesserungs-Tracking und DoD-Matrix.
  - Governance-Policy fuer Branch-/PR-Naming (`docs/governance/007_POLICY_BRANCH_PR_NAMING_DE.MD`).
- Changed:
  - `preflight` erzwingt jetzt fail-closed:
    - PR-Governance-Check (Branch/Titel/PR-Body),
    - `security/code-scanning/tools` muss 0 offene Alerts liefern.
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
