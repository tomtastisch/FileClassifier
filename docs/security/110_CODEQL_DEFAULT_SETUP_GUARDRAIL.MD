# CodeQL Default Setup Guardrail

## Purpose
This repository uses a **CodeQL Advanced Setup** (`.github/workflows/codeql.yml`) for C# (build mode `manual`).
GitHub **CodeQL Default Setup** must therefore be **disabled** (`state=not-configured`), otherwise advanced SARIF uploads can be rejected.

## Verification
```bash
gh api repos/{owner}/{repo}/code-scanning/default-setup --jq '{state,updated_at,schedule}'
```

Expected:
- `state: "not-configured"`

## Guardrails
- Merge gate (fail-closed): `tools/ci/check-codeql-default-setup.sh` is part of `preflight`.
- Drift detection: `.github/workflows/codeql-default-setup-guardrail.yml` runs daily and creates an issue on drift.

## Token / Permissions (important)
The GitHub API endpoint `GET /repos/{owner}/{repo}/code-scanning/default-setup` is not accessible in GitHub Actions with the default `GITHUB_TOKEN` in this repo (typical: `HTTP 403 Resource not accessible by integration`).

To make the guardrail work deterministically in PR CI and the scheduled workflow, a fine-grained PAT is required as a repo secret:
- Secret-Name: `CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN`
- Minimal permissions (repository permissions):
  - Administration: Read-only
  - Security events: Read-only

Wiring:
- PR CI: `.github/workflows/ci.yml` sets `CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN` as env for `preflight`.
- Scheduled: `.github/workflows/codeql-default-setup-guardrail.yml` uses `CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN` if present.
