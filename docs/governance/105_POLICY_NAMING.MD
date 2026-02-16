# Naming Policy (SNT Level 3)

## 1. Goal and Scope
This policy defines `Single Name Truth (SNT)` at level 3.
It applies to:
- GitHub repository name
- NuGet `PackageId`
- `PackageProjectUrl` / `RepositoryUrl`
- installation snippets in `README.md` and relevant `docs/*`
- CI/release artifacts and verification scripts

## 2. Canonical SSOT Tuple
The only normative source is:
- `tools/ci/policies/data/naming.json`

Canonical fields (schema `tools/ci/policies/data/naming.json`):
- `canonical_name`
- `product_name`
- `package_id`
- `assembly_name`
- `root_namespace`
- `repo_identity`
- `repo_slug`
- `repo_display_name`
- `repository_url`
- `deprecated_package_ids`
- `namespace_decision`
- `docs_expected_links`

Name changes are performed exclusively by updating this SSOT file plus an accompanying migration.

## 3. What MUST be consistent
Hard equality/consistency rule (SNT strict identity):
- `canonical_name == package_id == assembly_name == root_namespace`
- `repo_slug` must match the `origin` remote slug
- entries in `deprecated_package_ids` must not be used as canonical package references

The following locations MUST match the SSOT:
1. `src/FileTypeDetection/FileTypeDetectionLib.vbproj`
   - `PackageId == package_id`
   - `RepositoryUrl == repository_url`
   - `PackageProjectUrl == repository_url`
   - `AssemblyName == assembly_name`
   - `RootNamespace == root_namespace`
   - source namespace in `src/FileTypeDetection/**/*.vb` is unified under `root_namespace`
2. `README.md` and defined usage documents
   - installation examples use only `package_id`
   - IDs from `deprecated_package_ids` must not appear as install snippets
3. CI/release
   - `tools/ci/bin/run.sh` enforces exactly `package_id` in `run_pack`
   - `.github/workflows/release.yml` runs a naming gate before pack/publish
4. Package consumers in the repo
   - `samples/PortableConsumer/PortableConsumer.csproj`
   - `tests/PackageBacked.Tests/PackageBacked.Tests.csproj`
   - both reference only `package_id`

## 4. Enforcement (fail-closed)
Deterministic check:
- `tools/ci/check-naming-snt.sh`
  - reads SSOT
  - checks project metadata, doc references, and workflow/CI locations
  - writes machine-readable: `artifacts/ci/<check_id>/naming-snt-summary.json`

RaC rule (PolicyRunner):
- `tools/ci/policies/rules/naming_snt.yaml`
- `policy_type: naming_snt`
- on any deviation: `fail` (no soft bypass)
- evidence must include: file + exact match position (`file:line` or unique path)

## 5. NuGet Migration (level 3 operation)
Target state on nuget.org:
- Exactly ONE package is discoverable/recommended: `package_id`
- legacy packages from `deprecated_package_ids` remain technically present, but:
  - unlisted for all versions
  - deprecated with a pointer to `package_id`
  - no longer recommended in documentation

Operational steps (maintainers):
1. Release the new package (`package_id`).
2. Prepare/execute legacy migration:
   - dry run: `bash tools/ci/nuget-migrate-legacy-package.sh`
   - apply: `APPLY=1 NUGET_API_KEY=<key> bash tools/ci/nuget-migrate-legacy-package.sh`
3. Unlist the old package (`nuget delete` on nuget.org semantics = unlist).
4. Deprecate the old package and set the alternative package.
5. Fix documentation to the new package.
6. CI gates (`naming-snt`, `pack`, `summary`) must be green.

Note:
- If the local NuGet CLI does not support `deprecate`, deprecation is performed in the nuget.org UI (Manage Package -> Deprecation).
- The migration script aborts fail-closed in `APPLY=1` when deprecation cannot be automated.

## 6. Namespace Decision
The namespace strategy is explicitly stored in the SSOT (`namespace_decision`).
Current decision per SSOT:
- `mode: rename`
- `current_root_namespace: FileTypeDetection`
- `target_root_namespace: Tomtastisch.FileClassifier`

This change must be treated as a breaking change (major) and is accompanied by migration documentation.

## 7. Non-Goals
- No hard-delete assumption for nuget.org.
- No parallel \"recommended\" package names in docs or CI.
- No configuration-less special cases outside SSOT.

## 8. Post-Merge Release Control
After merge/tag release, the following is mandatory:
1. Run the release workflow for the tag.
2. Download artifact `release-naming-snt-summary`.
3. `naming-snt-summary.json` must contain `\"status\": \"pass\"`.
4. On deviation, treat the release as non-compliant.

## RoC References
- Naming SNT rule: `tools/ci/policies/rules/naming_snt.yaml`
- [Artifact contract rule](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs drift rule](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
