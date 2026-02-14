# Codex Operating Rules (Repository Contract)

## 1) Mission
- Work deterministic, evidence-based, and fail-closed.
- Prefer correctness, reproducibility, and security over speed.
- Never change `SECURITY.md`.

## 2) Mandatory Response Structure
Use this exact structure for substantial tasks:
1. GOAL & SCOPE
2. DEFINITIONS & ASSUMPTIONS
3. PLAN (STEPS + CHECKS)
4. EXECUTION (COMMANDS / FILE CHANGES)
5. VERIFICATION & EVIDENCE
6. RESULT & DECISION LOG
7. RISKS / OPEN ITEMS / NEXT STEPS (PRIORITIZED)

## 3) Iterative Delivery Model (Hard Process)
- WIP limit is 1: one topic per branch/PR.
- Branch naming must use `codex/tomtastisch-patch-<n>`.
- After each push:
  - Wait for required checks to finish.
  - Address review comments.
  - Resolve all threads, including outdated threads.
- Merge only if:
  - Required checks are green.
  - No unresolved review threads remain.
  - PR is mergeable by ruleset/branch protection.
- After merge:
  - Verify latest `main` run is green.
  - Start next topic in a new patch branch.

## 4) Fail-Closed Policy
- Blocker checks must pass; no silent bypass.
- `unknown` is allowed only for explicitly report-only claims.
- Live API checks are blocker-grade:
  - retries with backoff required,
  - explicit reason codes required,
  - final timeout/error => fail.

## 5) Evidence Requirements
- Every material claim needs evidence:
  - command,
  - artifact/log output,
  - file path.
- If unverifiable, mark as `ASSUMPTION` and add a concrete verification command.

## 6) Security and Workflow Guardrails
- Least privilege for GitHub Actions permissions:
  - top-level read-only,
  - write permissions only at job-level where required.
- Pin action references to immutable SHAs.
- No secret values in logs/output.
- Do not enable risky patterns like `pull_request_target` + secrets unless explicitly approved.

## 7) Versioning Convergence (Mandatory)
The following must stay equal by policy:
- `Directory.Build.props` -> `RepoVersion`
- `src/FileTypeDetection/FileTypeDetectionLib.vbproj` -> `Version`
- `src/FileTypeDetection/FileTypeDetectionLib.vbproj` -> `PackageVersion`
- Top entry in `docs/versioning/002_HISTORY_VERSIONS.MD`
- GitHub latest release tag version
- NuGet latest package version

If any mismatch exists, treat as blocker and fix before merge/release.

## 8) Scope and Safety Constraints
- Never revert unrelated user changes.
- Never use destructive git commands (`reset --hard`, `checkout --`) unless explicitly requested.
- Keep changes minimal and scoped to the current topic.
- If unexpected workspace drift is detected, stop and ask before proceeding.

## 9) Local Overrides
- If `AGENTS.override.md` exists locally, apply it in addition to this file.
- `AGENTS.override.md` is local-only and must not be committed.
