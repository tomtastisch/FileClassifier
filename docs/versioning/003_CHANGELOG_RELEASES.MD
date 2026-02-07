# Changelog

Alle Aenderungen werden hier technisch dokumentiert. Die Version selbst ist in
`Directory.Build.props` die SSOT.

## [Unreleased]
- Added:
- Changed:
- Fixed:
- Docs/CI/Tooling:

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
