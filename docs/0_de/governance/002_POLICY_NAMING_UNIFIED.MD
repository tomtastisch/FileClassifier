<!-- LANG_SWITCH:BEGIN -->
[DE](002_POLICY_NAMING_UNIFIED.MD) | [EN](../../1_en/governance/002_POLICY_NAMING_UNIFIED.MD)
<!-- LANG_SWITCH:END -->

# Einheitliche Naming-Richtlinie (SSOT)

## 1. Zweck
Diese Richtlinie definiert eine kanonische öffentliche Identität für Paketname, Assembly-Name, Namespace-Root, Doku-Install-Snippets und CI-Checks.

## 2. SSOT
Normative Quelle: `tools/ci/policies/data/naming.json`.

## 3. SHALL-Statements (kanonische Gleichheit)
- `canonical_name` SHALL gleich `package_id` sein.
- `canonical_name` SHALL gleich `assembly_name` sein.
- `canonical_name` SHALL gleich `root_namespace` sein.
- `canonical_name` SHALL als öffentlicher Namespace-Root in Source-Definitionen verwendet werden.
- Kanonischer Wert ist `Tomtastisch.FileClassifier`.

## 4. Regel für veraltete Pakete
- `deprecated_package_ids` SHALL veraltete Paket-IDs listen.
- Veraltete Paket-IDs (aus SSOT `deprecated_package_ids`) SHALL NICHT in Install-Snippets oder PackageReference-Beispielen erscheinen.
- Veraltete IDs MAY nur in Migrationsdokumentation erscheinen.

## 5. GitHub-Slug-Einschränkung
- GitHub-Repository-Slug ist operative Identität (`repo_identity`) und MAY von der Paket-ID abweichen.
- Aktueller Slug ist `FileClassifier`; kanonisches Paket bleibt `Tomtastisch.FileClassifier`.
- Falls literal gleiche Slugs erforderlich sind, sind manuelle Rename-Schritte notwendig:
  1. GitHub Repository Settings -> Rename repository.
  2. `repository_url` in `tools/ci/policies/data/naming.json` aktualisieren.
  3. `RepositoryUrl`/`PackageProjectUrl` in `src/FileTypeDetection/FileTypeDetectionLib.vbproj` aktualisieren.
  4. Lokale Remotes (`git remote set-url origin ...`) und CI-Referenzen aktualisieren.

## 6. CI-Enforcement-Mapping
- Rule-ID ist fix: `CI-NAMING-001`.
- Regeldatei: `tools/ci/policies/rules/naming_snt.yaml`.
- Checker: `tools/ci/check-naming-snt.sh`.
- Reports:
  - `artifacts/naming_snt_report.json`
  - `artifacts/naming_snt_report.tsv`
