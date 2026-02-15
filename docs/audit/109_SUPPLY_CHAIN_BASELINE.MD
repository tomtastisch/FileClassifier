# Supply Chain Baseline

## 1. Objective
Define minimum reproducible controls for source-to-package integrity in this repository.

## 2. Control Baseline
- S1 Source integrity:
  - branch protections and required status checks on default branch
  - deterministic CI gates (`preflight`, `build`, `security-nuget`, `summary`)
- S2 Build integrity:
  - `dotnet restore --locked-mode` in CI/release paths
  - fail-closed policy checks in `tools/ci/bin/run.sh`
- S3 Release integrity:
  - OIDC trusted publishing in `.github/workflows/release.yml`
  - provenance attestation via `actions/attest-build-provenance@v2`
- S4 Vulnerability hygiene:
  - NuGet vulnerability gate (`security-nuget`)
  - security claims verification (`security-claims-evidence`)

## 3. Evidence Mapping
- E1 CI workflow evidence:
  - `.github/workflows/ci.yml`
  - `artifacts/ci/*`
- E2 Security claim evidence:
  - `.github/workflows/security-claims-evidence.yml`
  - `artifacts/ci/security-claims-evidence/result.json`
- E3 Code analysis evidence:
  - `.github/workflows/code-analysis-evidence.yml`
  - `artifacts/ci/code-analysis-evidence/result.json`
- E4 Release/provenance evidence:
  - `.github/workflows/release.yml`
  - `artifacts/nuget/attestation-verify.txt` (when release workflow runs)

## 4. Verification Commands
All commands are intended to run from the repository root.
```bash
bash tools/ci/bin/run.sh security-nuget
bash tools/audit/verify-security-claims.sh
bash tools/audit/verify-code-analysis-evidence.sh
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
dotnet nuget verify "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
```

## 5. Operational Cadence
- Every PR: CI, security claims, and code-analysis evidence workflows
- Every release: attestation generation and verification
- Regular review: refresh baseline docs when controls or workflows change

## 6. Boundaries And Limits
- This baseline provides assurance evidence, not formal third-party certification.
- Downstream runtime hardening remains the responsibility of deployers/operators.
