# Audit-Index

## Zweck und Scope
Zentraler Index fuer Evidence-/Hardening-Dokumente, die Claims aus `SECURITY.md` nachweisbar machen, ohne `SECURITY.md` selbst zu aendern.
Root-Landing-Page fuer Dritte: `SECURITY_ASSURANCE_INDEX.md`.

## Dokumente
- `docs/audit/000_HASHING_BASELINE.MD`
- `docs/audit/000_INDEX.MD`
- `docs/audit/002_AUDIT_CONTRACT_AND_GUARDRAILS.MD`
- `docs/audit/003_SECURITY_ASSERTION_TRACEABILITY.MD`
- `docs/audit/004_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD`
- `docs/audit/005_CODE_ANALYSIS_METHOD.MD`
- `docs/audit/006_CODE_REVIEW_FINDINGS.MD`
- `docs/audit/007_THREAT_MODEL.MD`
- `docs/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD`
- `docs/audit/009_SUPPLY_CHAIN_BASELINE.MD`
- `docs/audit/010_REFACTOR_BACKLOG.MD`
- `docs/audit/011_SECURITY_BENCHMARK.MD`
- `docs/audit/012_WAVE_EXECUTION_DOD.MD`
- `docs/audit/013_SCORECARD_GOVERNANCE_ALERT_MAPPING.MD`
- `docs/audit/014_EVIDENCE_REPORT_ISSUE_67.MD`

## Maschinelle Evidence
- `artifacts/ci/security-claims-evidence/`
- `artifacts/ci/code-analysis-evidence/`
- `artifacts/audit/code_inventory.json`
- `artifacts/audit/callgraph_inventory.json`
- `artifacts/audit/dead_code_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `artifacts/audit/hardening_candidates.json`

## Repro-Kommandos
Alle Kommandos sind fuer Ausfuehrung im Repository-Root gedacht.
```bash
bash tools/audit/verify-security-claims.sh
bash tools/audit/verify-code-analysis-evidence.sh
bash tools/audit/generate-code-analysis-json.sh
python3 tools/check-docs.py
bash tools/versioning/verify-version-convergence.sh
```

## Externe Assurance
- OpenSSF Scorecard Workflow: `.github/workflows/scorecard.yml`
- Artifact Attestations im Release-Workflow: `.github/workflows/release.yml`
- Deep Analysis Evidence Workflow: `.github/workflows/code-analysis-evidence.yml`
- GitHub Code Scanning Default Setup (state muss `not-configured` sein): `gh api "repos/<owner>/<repo>/code-scanning/default-setup"`
