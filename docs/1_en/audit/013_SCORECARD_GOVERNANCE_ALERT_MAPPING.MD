<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/audit/013_SCORECARD_GOVERNANCE_ALERT_MAPPING.MD) | [EN](013_SCORECARD_GOVERNANCE_ALERT_MAPPING.MD)
<!-- LANG_SWITCH:END -->

# Scorecard Governance Alert Mapping (Ruleset/Process)

As of: 2026-02-13

## Verified Current State (2026-02-13)
- Branch protection `main`:
  - `required_pull_request_reviews.required_approving_review_count = 1`
  - `require_code_owner_reviews = true`
  - `require_last_push_approval = true`
- Fuzzing:
  - Workflow `.github/workflows/fuzzing-baseline.yml` active.
  - Last manual evidence run: `22003901268` (`success`).

## Goal
Map repo/governance-based Scorecard alerts without a concrete file (`no file associated`) to verifiable repository rules and processes.

## Scope
- `MaintainedID`
- `CodeReviewID`
- `BranchProtectionID`
- `FuzzingID`
- `CIIBestPracticesID`

## Out of Scope
- Code-line analyzer hints (e.g. `UseCollectionExpression`, `RedundantQualifier`).
- File-based alerts (`TokenPermissionsID`, `PinnedDependenciesID`), since those are fixed separately in code/workflows.

## Mapping (Claim -> Control -> Evidence)
| Alert ID | Control (target) | Current implementation | Evidence (deterministic) | DoD A | DoD B |
|---|---|---|---|---|---|
| `BranchProtectionID` | `main` only via PR + required checks | branch protection/ruleset active, required checks configured | `gh api repos/tomtastisch/FileClassifier/branches/main/protection` | `required_status_checks` present | direct push to `main` is technically blocked |
| `CodeReviewID` | at least 1 PR review before merge | PR review policy in branch protection/ruleset | `gh api repos/tomtastisch/FileClassifier/branches/main/protection --jq '.required_pull_request_reviews'` | `required_approving_review_count >= 1` | merge without review not possible |
| `MaintainedID` | evidence of active maintenance | continuous commits/releases + active CI | `gh api repos/tomtastisch/FileClassifier/commits?per_page=20` and `gh api repos/tomtastisch/FileClassifier/actions/runs?per_page=20` | commits exist in last 90 days | successful workflow runs exist in last 30 days |
| `FuzzingID` | fuzzing baseline exists (additionally enforced as a release blocker) | workflow `.github/workflows/fuzzing-baseline.yml` | `gh workflow view fuzzing-baseline.yml --yaml` and `gh run list --workflow fuzzing-baseline.yml --limit 10` | workflow exists and is executable | at least one successful run in last 30 days |
| `CIIBestPracticesID` | process/security baseline documented and traceable | audit/governance docs + CI evidence + security policy | `ls docs/audit` + `bash tools/audit/verify-security-claims.sh` | audit index exists and links | security claims evidence returns `pass` for blocker claims |

## Note on Scorecard Interpretation
- These alerts evaluate governance maturity, not only code content.
- An alert can remain open even if parts are implemented technically, if Scorecard weights public signals differently.
- For merge gates, only internally and deterministically defined blocker checks apply.

## Operational Process (fail-closed)
1. Weekly snapshot of open Scorecard alerts.
2. For each governance alert, run evidence commands and persist artifacts under `artifacts/security/`.
3. On drift (`fail`), correct ruleset/workflow immediately.
4. After corrections, re-run Scorecard + security-claims evidence.
5. Merge gate: `security/code-scanning/tools` must report `0` open alerts.
   - Default: gate checks `main` (prevents merges while `main` is red).
   - Exception for remediation PRs: PRs labeled `area:qodana` check alerts against the PR ref (`refs/pull/<nr>/merge`) to avoid deadlocks (a Qodana-fix PR can merge even if `main` is already red).
   - Implementation: `tools/ci/check-code-scanning-tools-zero.sh` (ref selection via PR label).
6. Heuristic alerts that cannot be remediated technically (e.g. repo age `<90 days`) are closed only with a justified dismissal comment and recorded in PR evidence.
