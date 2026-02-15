# Labeling Ownership

## 1. Scope
This governance policy applies to deterministic PR auto-labeling and auto-versioning behavior.

## 2. Owned Files
- `.github/workflows/ci.yml`
- `tools/versioning/*`
- `docs/versioning/001_POLICY_VERSIONING.MD`
- `docs/versioning/004_POLICY_LABELING.MD`

## 3. Change Requirements
Any change to taxonomy, priority, caps, or versioning decision logic must include:
- Updated docs
- Updated/added golden testcases
- Passing label engine validation

## 4. Review Policy
At least one maintainer owner review is required for the owned files.

## 5. Non-Goals
This policy does not change product runtime behavior; it governs repository automation only.

## RoC-Bezug
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
- [Shell-Safety-Regeln](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/shell_safety.yaml)
