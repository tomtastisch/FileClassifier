# CodeQL Default Setup Guardrail

## Ziel
Dieses Repository nutzt ein **CodeQL Advanced Setup** (`.github/workflows/codeql.yml`) fuer C# (build-mode `manual`).
GitHub **CodeQL Default Setup** muss deshalb **deaktiviert** sein (`state=not-configured`), sonst werden Advanced-SARIF Uploads abgelehnt.

## Verifikation
```bash
gh api /repos/tomtastisch/FileClassifier/code-scanning/default-setup --jq '{state,updated_at,schedule}'
```

Erwartung:
- `state: "not-configured"`

## Guardrails
- Merge-Gate (fail-closed): `tools/ci/check-codeql-default-setup.sh` ist Teil von `preflight`.
- Drift Detection: `.github/workflows/codeql-default-setup-guardrail.yml` laeuft taeglich und erstellt bei Drift ein Issue.

