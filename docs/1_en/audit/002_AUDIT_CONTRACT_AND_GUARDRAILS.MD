<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/audit/002_AUDIT_CONTRACT_AND_GUARDRAILS.MD) | [EN](002_AUDIT_CONTRACT_AND_GUARDRAILS.MD)
<!-- LANG_SWITCH:END -->

# Audit Contract And Guardrails

## 1. Purpose
Define deterministic evidence rules for `SECURITY.md` claim verification.

## 2. Status Model
Result contract status values:
- `pass`: no blocking violations.
- `fail`: at least one blocking violation.
- `warn`: report-only findings.

Operational tri-state mapping used by claim engine:
- `pass` -> schema `pass`
- `fail` -> schema `fail`
- `unknown` -> schema `warn` with explicit reason code in `rule_violations[].message`

## 3. Blocking Policy
Blocking checks are allowed only if deterministic.
Live GitHub API checks are blocker-eligible with strict anti-flake controls:
- 3 retries
- exponential backoff (2s, 4s, 8s)
- current implementation emits generic API failure messages after retries

If retries are exhausted:
- blocker claims fail with reason code
- report-only claims emit warn

## 4. Evidence Contract
Required artifacts per check:
- `raw.log`
- `summary.md`
- `result.json` (must match `tools/ci/schema/result.schema.json`)

## 5. Drift Handling
- Policy drift is fail-closed for blocker claims.
- Unknown states are never silently ignored.
- Every failure must include an evidence path and a reproducible command.
