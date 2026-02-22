<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/audit/012_WAVE_EXECUTION_DOD.MD) | [EN](012_WAVE_EXECUTION_DOD.MD)
<!-- LANG_SWITCH:END -->

# Wave Execution DoD Matrix

As of: 2026-02-13
Scope: finalize all open security/governance items per wave plan.

## Cluster 0 - Guardrails & Contract
- DoD-0.1: `tools/ci/schema/result.schema.json` validates all produced `result.json` in CI without special-case bypass.
- DoD-0.2: For each blocking check there is a unique fail reason in the artifact (`rule_id`, `message`, `evidence_paths`).

## Cluster 1 - SECURITY Truth Matrix
- DoD-1.1: Every statement in `SECURITY.md` has a `SEC-CLAIM-*` mapping in `docs/audit/103_SECURITY_ASSERTION_TRACEABILITY.MD`.
- DoD-1.2: Every blocker-classified statement has at least one reproducible command with an `exit 0/!=0` criterion.

## Cluster 2 - Evidence Engine
- DoD-2.1: `bash tools/audit/verify-security-claims.sh` produces schema-valid `raw.log`, `summary.md`, `result.json`.
- DoD-2.2: API errors are classified into reason codes (`auth|rate-limit|network|5xx|unknown`).

## Cluster 3 - CI Integration
- DoD-3.1: `security-claims-evidence` and `code-analysis-evidence` run on `push` and `pull_request` against `main`.
- DoD-3.2: Blocker claims fail deterministically; report-only claims do not block merges.

## Cluster 4 - Externe Audits/Attestations
## Cluster 4 - External Audits/Attestations
- DoD-4.1: OpenSSF Scorecard workflow produces SARIF and uploads artifacts.
- DoD-4.2: Release workflow generates and verifies build attestations (`gh attestation verify`).

## Cluster 5 - Sichtbarkeit Repo/NuGet
## Cluster 5 - Repo/NuGet Visibility
- DoD-5.1: Root evidence file `SECURITY_ASSURANCE_INDEX.md` exists and links all core evidence.
- DoD-5.2: README contains a visible `Security Assurance & Evidence` section with audit/runbook/threat links.

## Cluster 6 - CodeQL / Scanning Quality
- DoD-6.1: For each main commit, `Analyze (actions|csharp|javascript-typescript|python)` completes successfully.
- DoD-6.2: No parallel incompatible CodeQL upload path (default setup documented/applied conflict-free).

## Cluster 7 - Deep Analysis (JSON-only)
- DoD-7.1: JSON artifacts `code_inventory|callgraph_inventory|dead_code_candidates|redundancy_candidates|hardening_candidates` are produced.
- DoD-7.2: Findings with file+line are referenced reproducibly in `docs/audit/106_CODE_REVIEW_FINDINGS.MD`.

## Cluster 8 - Governance Closure
- DoD-8.1: Threat model, incident runbook, and supply-chain baseline exist and are mutually consistent.
- DoD-8.2: Documents reference real, existing workflows/artifacts without placeholders.

## Snapshot Warning Hardening (Dependency Review)
- DoD-SW.1: `dependency-review` uses `retry-on-snapshot-warnings: true`.
- DoD-SW.2: PR comment output happens only on failure (`comment-summary-in-pr: on-failure`) so warnings do not become persistent noise.
- DoD-SW.3: The OpenSSF scorecard table in PR comments is disabled (`show-openssf-scorecard: false`) to avoid propagating upstream scores as merge noise.
- DoD-SW.4: The policy threshold for action score is explicitly set (`warn-on-openssf-scorecard-level: 9`) and evidenced in the workflow.

## Release Finalisierung
- DoD-R.1: After merge, tag `v5.1.0` is created and the release pipeline completes successfully.
- DoD-R.2: `git version == nupkg version == nuget version` is evidenced in the release artifact.
