<!-- LANG_SWITCH:BEGIN -->
[DE](001_POLICY_VERSIONING.MD) | [EN](101_POLICY_VERSIONING.MD)
<!-- LANG_SWITCH:END -->

# Versioning and Release Policy (SSOT)

## 1. Purpose
This policy defines binding SemVer rules (`MAJOR.MINOR.PATCH`) and deterministic CI enforcement.

## 2. Scope
- Release tags `vX.Y.Z` (optional `-prerelease`) are the only SSOT source for package versions.
- PR versioning guard and auto labeling in CI.

## 3. Terms
- SSOT: single source of truth for version information.
- Breaking change: change that can break existing integrations without adjustments.

## 4. SemVer Rules (binding)
- `MAJOR`
  - public API removed/renamed
  - incompatible default behavior
- `MINOR`
  - new backward-compatible features or API extensions
- `PATCH`
  - fixes, refactorings without behavioral break, tests, docs, CI, tooling
- `NONE`
  - metadata-only changes without version impact
- Pre-release tags are allowed:
  - `vX.Y.Z-<label>` is taken over exactly as NuGet version `X.Y.Z-<label>`.

## 5. Decision Matrix
| Change type | Example | Bump |
|---|---|---|
| Public API rename/remove | `ZipProcessing` -> `ArchiveProcessing` | MAJOR |
| Breaking defaults/policy | stricter behavior without opt-in | MAJOR |
| Compatible feature | new datatype/flow | MINOR |
| Fix/refactor | stabilization, cleanup | PATCH |
| Docs/tests/CI/tooling | workflow/script/test/doc update | PATCH |
| Metadata-only | mechanical change without runtime impact | NONE |

## 6. CI Guard (mandatory)
- `tools/versioning/check-version-policy.sh` enforces fail-closed:
  - In PR/CI: no static version fields in `Directory.Build.props`/`Directory.Build.targets`/`*.csproj`/`*.vbproj`.
  - In release (tag): tag format validation and `TagVersion == nupkg-PackageVersion`.
- Publishing is allowed only on `refs/tags/v*`.
- Target feed for online installability: NuGet.org.
- Release packaging sets the version exclusively via:
  - `dotnet pack ... -p:PackageVersion=$VERSION -p:Version=$VERSION`
- Assembly/file version are derived deterministically from the tag core:
  - `AssemblyVersion = MAJOR.MINOR.0.0`
  - `FileVersion = MAJOR.MINOR.PATCH.0`
- `tools/versioning/check-versioning.sh` remains for determinism of the `required` classification in PR labeling.

## 7. Deterministic Auto Labeling (mandatory)
Caps per PR run:
- exactly one `version:*`
- exactly one primary label
- at most one `impl:*`
- at most two `area:*`

All outdated auto labels are removed before applying new labels.

## 8. Label Registry
### 8.1 Version Labels
- `version:major`
- `version:minor`
- `version:patch`
- `version:none`

### 8.2 Primary Labels
- `breaking`, `feature`, `fix`, `refactor`, `ci`, `test`, `docs`, `tooling`, `chore`

### 8.3 Implementation Labels
- `impl:quality`, `impl:security`, `impl:docs`, `impl:config`

### 8.4 Area Labels
- `area:pipeline`, `area:qodana`, `area:archive`, `area:hashing`, `area:detection`,
- `area:materializer`, `area:versioning`, `area:tests`, `area:docs`, `area:tooling`

Canonical source:
- `tools/versioning/labels.json`

## 9. Priority Rule
Primary priority is fixed:
`breaking > feature > fix > refactor > ci > test > docs > tooling > chore`

## 10. Related SSOT Documents
- `docs/versioning/104_POLICY_LABELING.MD`
- `docs/ci/101_PIPELINE_CI.MD`
- `docs/governance/102_POLICY_LABELING.MD`
- `docs/governance/104_POLICY_DOCUMENTATION.MD`

## 11. Qodana
Qodana is additional static analysis and does not replace CI quality gates for build/test/coverage/versioning.

## RoC References
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
- [Shell-Safety-Regeln](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/shell_safety.yaml)
