# Threat Model

## 1. Scope
In scope:
- library behavior in `src/FileTypeDetection/*`
- CI/CD and release controls in `.github/workflows/*`
- security reporting and evidence lifecycle in `SECURITY.md` and `docs/audit/*`

Out of scope:
- downstream deployment hardening of consuming systems
- host-level controls outside this repository

## 2. Assets
- A1: untrusted file payloads entering detection/extraction paths
- A2: extracted archive entries in memory
- A3: hash/evidence artifacts used for integrity assertions
- A4: produced NuGet package (`.nupkg`) and its provenance
- A5: vulnerability reports and coordinated disclosure state

## 3. Trust Boundaries
- B1: caller input -> detector/archive internals (`src/FileTypeDetection/FileTypeDetector.vb`)
- B2: repository source -> CI execution (`.github/workflows/ci.yml`)
- B3: CI-produced package -> NuGet publication (`.github/workflows/release.yml`)
- B4: external reporter -> maintainer triage (GitHub private vulnerability reporting)

## 4. Attacker Capabilities
- C1: submit crafted files/archives to trigger unsafe parsing or extraction logic
- C2: attempt path traversal or resource exhaustion through archive payloads
- C3: attempt supply-chain tampering between build and publication
- C4: attempt disclosure abuse via public channels before fix readiness

## 5. Threat Scenarios And Controls
- T1: archive traversal (zip-slip style path escape)
  - Impact: write/read outside intended scope during extraction.
  - Current controls: fail-closed archive safety gates in archive internals and detector paths; extraction APIs constrained to safe flows.
  - Evidence: `tests/FileTypeDetectionLib.Tests/*` plus CI job `tests-bdd-coverage` in `.github/workflows/ci.yml`.
- T2: decompression/resource exhaustion
  - Impact: memory/CPU pressure, possible denial-of-service.
  - Current controls: guarded archive processing paths and bounded validation/extraction flows.
  - Evidence: source paths under `src/FileTypeDetection/Infrastructure/*` and CI execution traces.
- T3: release artifact provenance tampering
  - Impact: consumers install untrusted package build output.
  - Current controls: OIDC trusted publishing and provenance attestation in `.github/workflows/release.yml`.
  - Evidence: release workflow step `actions/attest-build-provenance@v2` and `gh attestation verify` output artifact.
- T4: vulnerable dependency ingress
  - Impact: known vulnerable transitive components in shipped package.
  - Current controls: `security-nuget` gate in CI (`dotnet list ... --vulnerable --include-transitive`) with High/Critical fail behavior.
  - Evidence: `tools/ci/bin/run.sh` (`run_security_nuget`) and CI artifacts `artifacts/ci/security-nuget/*`.
- T5: uncoordinated public disclosure before patch
  - Impact: exploitability window increases before mitigation is available.
  - Current controls: private vulnerability reporting path and coordinated disclosure process as declared in `SECURITY.md`.
  - Evidence: `tools/audit/verify-security-claims.sh` claim checks and `docs/audit/108_INCIDENT_RESPONSE_RUNBOOK.MD`.

## 6. Assumptions
- Maintainer access controls and repository permissions remain correctly configured.
- GitHub-hosted security features remain available for the repository.
- Consumers execute their own runtime hardening controls; this repository cannot enforce them.

## 7. Residual Risks
- R1: runtime denial-of-service risk can only be reduced, not eliminated, for hostile inputs.
- R2: organizational response speed depends on maintainer availability.
- R3: third-party platform outages can delay triage/release actions.

## 8. Reproducible Verification
All commands are intended to run from the repository root.
```bash
bash tools/audit/verify-security-claims.sh
bash tools/ci/bin/run.sh security-nuget
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
```
