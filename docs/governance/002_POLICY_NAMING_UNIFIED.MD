<!-- LANG_SWITCH:BEGIN -->
[DE](002_POLICY_NAMING_UNIFIED.MD) | [EN](102_POLICY_NAMING_UNIFIED.MD)
<!-- LANG_SWITCH:END -->

# Unified Naming Policy (SSOT)

## 1. Purpose
This policy defines one canonical public identity for package naming, assembly naming, namespace root, docs install snippets, and CI checks.

## 2. SSOT
Normative source: `tools/ci/policies/data/naming.json`.

## 3. SHALL Statements (Canonical Equality)
- `canonical_name` SHALL equal `package_id`.
- `canonical_name` SHALL equal `assembly_name`.
- `canonical_name` SHALL equal `root_namespace`.
- `canonical_name` SHALL be the public root namespace in source declarations.
- Canonical value is `Tomtastisch.FileClassifier`.

## 4. Deprecated Package Rule
- `deprecated_package_ids` SHALL list legacy package IDs.
- Deprecated package IDs (from SSOT `deprecated_package_ids`) SHALL NOT appear in install snippets or PackageReference examples.
- Deprecated IDs MAY appear only in migration documentation.

## 5. GitHub Slug Limitation
- GitHub repository slug is operational identity (`repo_identity`) and MAY differ from package ID.
- Current slug is `FileClassifier`; canonical package remains `Tomtastisch.FileClassifier`.
- If literal slug equality is required, manual rename steps are required:
  1. GitHub repository Settings -> Rename repository.
  2. Update `repository_url` in `tools/ci/policies/data/naming.json`.
  3. Update `RepositoryUrl`/`PackageProjectUrl` in `src/FileTypeDetection/FileTypeDetectionLib.vbproj`.
  4. Update local remotes (`git remote set-url origin ...`) and CI references.

## 6. CI Enforcement Mapping
- Rule ID is fixed: `CI-NAMING-001`.
- Rule file: `tools/ci/policies/rules/naming_snt.yaml`.
- Checker: `tools/ci/check-naming-snt.sh`.
- Reports:
  - `artifacts/naming_snt_report.json`
  - `artifacts/naming_snt_report.tsv`
