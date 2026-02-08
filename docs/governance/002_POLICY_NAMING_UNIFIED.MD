# Unified Naming Policy (SSOT)

## 1. Zweck
Diese Policy definiert die verbindliche Namensidentitaet fuer Paket, Namespace, Assembly und Dokumentation.

## 2. SSOT
Die einzige normative Quelle ist `tools/ci/policies/data/naming.json`.

## 3. Verbindliche Gleichheit
Die folgende Gleichheit MUSS gelten:
- `canonical_name == package_id == root_namespace == assembly_name`

Aktuell ist der kanonische Name:
- `Tomtastisch.FileClassifier`

## 4. Repo-Slug Relaxation
- Der GitHub-Repo-Slug DARF von `canonical_name` abweichen.
- Wenn `repo_slug != canonical_name`, dann MUESSEN in `src/FileTypeDetection/FileTypeDetectionLib.vbproj` gelten:
  - `RepositoryUrl == repository_url`
  - `PackageProjectUrl == repository_url`
- CI MUSS erzwingen, dass `repository_url` dem `git remote origin` entspricht (normalisiert).

## 5. Doku-Anforderung
- `repo_display_name` MUSS in README/docs als Produktname verwendet werden.
- Installationsbeispiele MUESSEN `package_id` verwenden.

## 6. Legacy-Pakete
- `deprecated_package_ids` MUESSEN vorhanden sein.
- Werte in `deprecated_package_ids` sind strikt legacy-only und duerfen nicht als kanonischer Installname verwendet werden.
- Legacy-Pakete sind unlist/deprecate zu migrieren.

## 7. Enforcement
- CI-Regel-ID bleibt: `CI-NAMING-001`.
- Regel ist fail-closed.
- Der Checker `tools/ci/check-naming-snt.sh` erzeugt:
  - `artifacts/naming_snt_report.json`
  - `artifacts/naming_snt_report.txt`
  - plus CI-summary JSON unter `artifacts/ci/naming-snt/`.

## 8. Migration
- Namespace-Rename auf `Tomtastisch.FileClassifier` ist eine Level-3 Public-Surface-Aenderung.
- Alte NuGet-ID (`Tomtastisch.FileTypeDetection`) bleibt nur als deprecated/unlisted Legacy-Artefakt.
- Migration-Hinweis fuer Consumers:
  - Bisher: `Tomtastisch.FileTypeDetection` (deprecated/unlisted)
  - Neu: `Tomtastisch.FileClassifier`

## RoC-Bezug
- Naming-SNT-Regel: `tools/ci/policies/rules/naming_snt.yaml`
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
