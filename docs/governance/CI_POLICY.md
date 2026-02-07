# CI Policy (SSOT)

## Scope
This document defines policy principles, severity handling, and exit code policy.
Normative policy definitions live in:
- `tools/ci/policies/schema/rules.schema.json`
- `tools/ci/policies/rules/`

## Global Rules
- Fail-closed: no silent bypass paths.
- No `continue-on-error: true` in workflow files.
- No `|| true` on critical workflow paths.
- No `set +e` without explicit allow-list entry.
- Workflow YAML only calls entry scripts under `tools/ci/bin/`.

## Result Contract
All required checks MUST write:
- `artifacts/ci/<check_id>/raw.log`
- `artifacts/ci/<check_id>/summary.md`
- `artifacts/ci/<check_id>/result.json`

`result.json` must comply with `tools/ci/schema/result.schema.json`.

## Rule Catalog
- `CI-ARTIFACT-001` fail: required artifact missing.
- `CI-POLICY-001` fail: policy rule loading/schema validation failed.
- `CI-SCHEMA-001` fail: `result.json` schema validation failed.
- `CI-SHELL-001` fail: found `continue-on-error: true`.
- `CI-SHELL-002` fail: found `|| true` in critical workflow path.
- `CI-SHELL-003` fail: found `set +e` outside allow-list.
- `CI-SHELL-004` fail: workflow `run: |` block exceeds configured max lines.
- `CI-GRAPH-001` fail: required CI graph edge or job constraint violated.
- `CI-QODANA-001` fail: `QODANA_TOKEN` missing.
- `CI-QODANA-002` fail: expected SARIF missing.
- `CI-QODANA-003` fail: SARIF invalid JSON.

## Severity Rules
- `warn`: visible, non-blocking.
- `fail`: blocking, exit code non-zero.

## Exit Code Matrix
- `0`: success (`pass` or `warn`)
- `1`: policy/contract/check failure (`fail`)
- `2`: invalid invocation or missing prerequisites

## set +e Allow-list
No allow-list entries in Phase 1.
