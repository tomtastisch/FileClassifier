# CI Policy (SSOT)

## Scope
This document defines policy principles, severity handling, and exit code policy.
Normative policy definitions live in:
- `tools/ci/policies/schema/rules.schema.json`
- `tools/ci/policies/rules/`
Entry index:
- `docs/governance/POLICY_INDEX.md`

## Global Rules
- Fail-closed: no silent bypass paths.
- Workflow YAML only calls entry scripts under `tools/ci/bin/`.

## Result Contract
All required checks MUST write:
- `artifacts/ci/<check_id>/raw.log`
- `artifacts/ci/<check_id>/summary.md`
- `artifacts/ci/<check_id>/result.json`

`result.json` must comply with `tools/ci/schema/result.schema.json`.

## Rule Catalog
- Rule IDs and file ownership are indexed in `docs/governance/POLICY_INDEX.md`.
- Evaluation details and thresholds are defined only in `tools/ci/policies/rules/`.

## Severity Rules
- `warn`: visible, non-blocking.
- `fail`: blocking, exit code non-zero.

## Exit Code Matrix
- `0`: success (`pass` or `warn`)
- `1`: policy/contract/check failure (`fail`)
- `2`: invalid invocation or missing prerequisites

## Allow-lists
Allow-list definitions are managed in rule parameters under `tools/ci/policies/rules/`.

## RoC-Bezug
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/90a2825/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/90a2825/tools/ci/policies/rules/docs_drift.yaml)
- [Shell-Safety-Regeln](https://github.com/tomtastisch/FileClassifier/blob/90a2825/tools/ci/policies/rules/shell_safety.yaml)
