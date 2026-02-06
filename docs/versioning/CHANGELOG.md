# Changelog

Alle Aenderungen werden hier technisch dokumentiert. Die Version selbst ist in
`Directory.Build.props` die SSOT.

## [Unreleased]
- BREAKING: Version-Baseline auf `4.0.0` angehoben (Major-Bump durch oeffentliche API-/Struktur-Aenderungen im Branch).
- Added:
- Changed:
- Fixed:
- Docs/CI/Tooling:
  - CI in getrennte, auditierbare Jobs refaktoriert (Preflight, Build, Security, Tests+Coverage, Summary).
  - BDD-Readable Testlauf liefert TRX + lesbare Ausgabe + Coverage + Gate im Single-Run.
  - Dokumentation fuer CI-Nachweise unter `docs/CI_PIPELINE.md` ergaenzt.

## [3.0.0]
- Breaking: Rename `FileTypeDetectorOptions` -> `FileTypeProjectOptions`
- Breaking: Rename `FileTypeSecurityBaseline` -> `FileTypeProjectBaseline`

## [2.0.0]
- Breaking: Rename `ZipProcessing` -> `ArchiveProcessing`

## [1.0.0]
- Initial implementation
