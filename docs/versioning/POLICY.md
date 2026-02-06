# Versioning & Release Policy

## 1. Purpose
This policy defines strict SemVer (`MAJOR.MINOR.PATCH`) and deterministic CI enforcement.

## 2. Terms
- SSOT: `Directory.Build.props` is the only source for current version.
- Breaking Change: public API/behavior/default changes that break existing integrations.

## 3. SemVer Rules (Binding)
- MAJOR
  - rename/remove public API
  - breaking default behavior changes
- MINOR
  - new compatible features, datatypes, or API surface
- PATCH
  - fixes, refactoring without behavior change, tests, docs, CI, tooling
- NONE
  - pure metadata changes with no version impact

## 4. Decision Matrix
| Change Type | Example | Bump |
|---|---|---|
| Public API rename/remove | `ZipProcessing` -> `ArchiveProcessing` | MAJOR |
| Breaking defaults/policy | stricter behavior without opt-in | MAJOR |
| Compatible feature | new supported format/model | MINOR |
| Bugfix/refactor | stabilization, cleanup | PATCH |
| Docs/Test/CI/Tooling | docs, test, workflows, scripts | PATCH |
| Metadata-only | mechanical/meta-only update | NONE |

## 5. CI Guard (Mandatory)
- `tools/versioning/check-versioning.sh` classifies changes and verifies required bump.
- `versioning-guard` must pass before build/test jobs.

## 6. Deterministic Auto-Labeling (Mandatory)
The PR auto-labeler runs in CI and follows strict caps:
- exactly one `version:*`
- exactly one primary label (`breaking|feature|fix|refactor|ci|test|docs|tooling|chore`)
- at most one `impl:*`
- at most two `area:*`

No extra keyword label group exists.

All stale auto-labels are removed before new labels are applied.

## 7. Label Registry
### 7.1 Version labels
- `version:major`
- `version:minor`
- `version:patch`
- `version:none`

### 7.2 Primary labels
- `breaking`, `feature`, `fix`, `refactor`, `docs`, `test`, `ci`, `tooling`, `chore`

### 7.3 Implementation labels
- `impl:quality`, `impl:security`, `impl:docs`, `impl:config`

### 7.4 Area labels
- `area:pipeline`, `area:qodana`, `area:archive`, `area:hashing`, `area:detection`,
  `area:materializer`, `area:versioning`, `area:tests`, `area:docs`, `area:tooling`

Canonical registry source:
- `tools/versioning/labels.json`

## 8. Primary Priority (Fixed)
`breaking > feature > fix > refactor > ci > test > docs > tooling > chore`

## 9. Console Evidence (Mandatory)
CI outputs deterministic versioning evidence:
- `required=<...>`
- `actual=<...>`
- `reason=<...>`

## 10. Documentation & Governance
- Auto-labeling/auto-versioning SSOT: `docs/AUTO_LABELING_AND_VERSIONING.md`
- Ownership rules: `docs/governance/LABELING_OWNERSHIP.md`
- CI pipeline overview: `docs/CI_PIPELINE.md`

## 11. Qodana
Qodana is integrated as static analysis workflow and complements CI quality gates.
