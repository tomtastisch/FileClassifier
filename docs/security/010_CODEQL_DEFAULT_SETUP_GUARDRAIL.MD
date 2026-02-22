<!-- LANG_SWITCH:BEGIN -->
[DE](010_CODEQL_DEFAULT_SETUP_GUARDRAIL.MD) | [EN](110_CODEQL_DEFAULT_SETUP_GUARDRAIL.MD)
<!-- LANG_SWITCH:END -->

# CodeQL Default Setup Guardrail

## Ziel
Dieses Repository nutzt ein **CodeQL Advanced Setup** (`.github/workflows/codeql.yml`) für C# (build-mode `manual`).
GitHub **CodeQL Default Setup** muss deshalb **deaktiviert** sein (`state=not-configured`), sonst werden Advanced-SARIF Uploads abgelehnt.

## Verifikation
```bash
gh api repos/{owner}/{repo}/code-scanning/default-setup --jq '{state,updated_at,schedule}'
```

Erwartung:
- `state: "not-configured"`

## Guardrails
- Merge-Gate (fail-closed): `tools/ci/check-codeql-default-setup.sh` ist Teil von `preflight`.
- Drift Detection: `.github/workflows/codeql-default-setup-guardrail.yml` läuft täglich und erstellt bei Drift ein Issue.

## Token / Permissions (wichtig)
Der GitHub API Endpoint `GET /repos/{owner}/{repo}/code-scanning/default-setup` ist in GitHub Actions mit dem standardmäßigen `GITHUB_TOKEN` in diesem Repo nicht erreichbar (typisch: `HTTP 403 Resource not accessible by integration`).

Damit der Guardrail in PR-CI und im scheduled Workflow deterministisch funktioniert, ist ein Fine-Grained PAT als Repo-Secret erforderlich:
- Secret-Name: `CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN`
- Minimalrechte (Repository permissions):
  - Administration: Read-only
  - Security events: Read-only

Wiring:
- PR-CI: `.github/workflows/ci.yml` setzt `CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN` als env für `preflight`.
- Scheduled: `.github/workflows/codeql-default-setup-guardrail.yml` nutzt `CODEQL_DEFAULT_SETUP_GUARDRAIL_TOKEN`, falls vorhanden.
