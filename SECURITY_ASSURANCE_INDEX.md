# Security Assurance Index

This index is the root-level entry point for reproducible security evidence that backs policy claims in `SECURITY.md`.

## Implemented Evidence
- Security claims evidence: `artifacts/ci/security-claims-evidence/`
- Code analysis evidence: `artifacts/ci/code-analysis-evidence/`
- Audit document index: `docs/audit/000_INDEX.MD`
- Claim traceability matrix: `docs/audit/003_SECURITY_ASSERTION_TRACEABILITY.MD`
- Certification/attestation roadmap: `docs/audit/004_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD`
- Threat model: `docs/audit/007_THREAT_MODEL.MD`
- Incident response runbook: `docs/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD`
- Supply chain baseline: `docs/audit/009_SUPPLY_CHAIN_BASELINE.MD`

## External Assurance
- OpenSSF Scorecard workflow: `.github/workflows/scorecard.yml`
- Artifact attestation in release flow: `.github/workflows/release.yml`
- Dependency review workflow: `.github/workflows/dependency-review.yml`

## Verification Commands
```bash
bash tools/audit/verify-security-claims.sh
bash tools/audit/verify-code-analysis-evidence.sh
gh api repos/tomtastisch/FileClassifier/code-scanning/alerts?state=open&per_page=100 --paginate
gh attestation verify artifacts/nuget/*.nupkg --repo tomtastisch/FileClassifier
```

## Coming Soon
- Expanded fuzzing corpus and dedicated fuzz regressions (Cluster 7)
- Formal external certification path (outside repository scope)
