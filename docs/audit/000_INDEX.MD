# Audit Index

## Scope
Central index for evidence-hardening artifacts that support claims in `SECURITY.md` without modifying `SECURITY.md` itself.
Root landing page for third parties: `SECURITY_ASSURANCE_INDEX.md`.

## Documents
- `docs/audit/000_HASHING_BASELINE.MD`
- `docs/audit/000_INDEX.MD`
- `docs/audit/002_AUDIT_CONTRACT_AND_GUARDRAILS.MD`
- `docs/audit/011_SECURITY_BENCHMARK.MD`
- `docs/audit/012_WAVE_EXECUTION_DOD.MD`
- `docs/audit/003_SECURITY_ASSERTION_TRACEABILITY.MD`
- `docs/audit/004_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD`
- `docs/audit/005_CODE_ANALYSIS_METHOD.MD`
- `docs/audit/006_CODE_REVIEW_FINDINGS.MD`
- `docs/audit/007_THREAT_MODEL.MD`
- `docs/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD`
- `docs/audit/009_SUPPLY_CHAIN_BASELINE.MD`
- `docs/audit/010_REFACTOR_BACKLOG.MD`
- `docs/audit/013_SCORECARD_GOVERNANCE_ALERT_MAPPING.MD`

## Machine Evidence
- `artifacts/ci/security-claims-evidence/`
- `artifacts/ci/code-analysis-evidence/`
- `artifacts/audit/code_inventory.json`
- `artifacts/audit/callgraph_inventory.json`
- `artifacts/audit/dead_code_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `artifacts/audit/hardening_candidates.json`

## Repro Commands
All commands are intended to run from the repository root.
```bash
bash tools/audit/verify-security-claims.sh
bash tools/audit/verify-code-analysis-evidence.sh
bash tools/audit/generate-code-analysis-json.sh
python3 tools/check-docs.py
bash tools/versioning/verify-version-convergence.sh
```

## External Assurance
- OpenSSF Scorecard workflow: `.github/workflows/scorecard.yml`
- Artifact attestations in release workflow: `.github/workflows/release.yml`
- Deep analysis evidence workflow: `.github/workflows/code-analysis-evidence.yml`
- GitHub Code Scanning Default Setup (configured): `gh api "repos/<owner>/<repo>/code-scanning/default-setup"`
