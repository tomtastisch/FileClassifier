# CI Pipeline (SSOT)

## Purpose
Deterministic and auditable CI with contract-first artifacts and strict fail-closed execution.

## Required Jobs
- `preflight`
- `build`
- `security-nuget`
- `tests-bdd-coverage`
- `summary`
- `qodana` (separate workflow)

## Artifact Root (single SSOT)
- `artifacts/ci/<check_id>/raw.log`
- `artifacts/ci/<check_id>/summary.md`
- `artifacts/ci/<check_id>/result.json`
- `artifacts/ci/qodana/*.sarif`

No alternative artifact roots are allowed.

## Stage Order
1. Preflight: governance-safe checks and policy guards.
2. Build: restore + build with warnings as errors.
3. Security: NuGet vulnerability/deprecation scan.
4. Tests: BDD + coverage gate.
5. Summary: aggregate and enforce artifact contract + schema validation.
6. Qodana workflow: token/sarif contract and dead-code gate.

## Workflow Constraints
- Workflow YAML contains entry-calls only.
- Check logic is implemented in `.NET` validators under `tools/ci/checks/`.
- Shell scripts in `tools/ci/` handle orchestration and artifact handling only.
