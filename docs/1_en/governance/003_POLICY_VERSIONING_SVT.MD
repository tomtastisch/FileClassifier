<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/governance/003_POLICY_VERSIONING_SVT.MD) | [EN](003_POLICY_VERSIONING_SVT.MD)
<!-- LANG_SWITCH:END -->

# Versioning Policy (SVT)

## 1. Purpose
This policy enforces Single Version Truth (SVT) for the canonical package release pipeline.

## 2. SSOT
Normative source: `tools/ci/policies/data/versioning.json`.

## 3. SVT Definition
For canonical package `Tomtastisch.FileClassifier`, the following SHALL match exactly:
- `git tag vX.Y.Z`
- vbproj `<Version>X.Y.Z</Version>`
- vbproj `<PackageVersion>X.Y.Z</PackageVersion>`
- produced `.nupkg` version `X.Y.Z`
- published NuGet version `X.Y.Z` (release job)

## 4. SHALL Statements
- Release tags SHALL use prefix `v` and semantic version core (`vMAJOR.MINOR.PATCH`).
- Release pack step SHALL derive `VERSION=${GITHUB_REF_NAME#v}`.
- `dotnet pack` SHALL pass `-p:Version=$VERSION -p:PackageVersion=$VERSION`.
- CI SHALL fail-closed on ambiguity (for example multiple matching tags on HEAD).
- Legacy package versions are out of canonical SVT scope and SHALL be deprecated/unlisted.

## 5. CI Enforcement Mapping
- Rule ID: `CI-VERSION-001`.
- Rule file: `tools/ci/policies/rules/versioning_svt.yaml`.
- Checker: `tools/ci/check-versioning-svt.sh`.
- Report: `artifacts/versioning_report.json`.
