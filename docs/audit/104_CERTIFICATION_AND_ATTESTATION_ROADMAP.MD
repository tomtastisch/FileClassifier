# Certification And Attestation Roadmap

## 1. Positioning
This project distinguishes:
- Assurance/attestation evidence (technical, reproducible)
- Formal third-party certification (external audit, out of current scope)

No formal certification claim is made by this repository.

## 2. Minimum Set (Wave 1)
1. OpenSSF Scorecard (free, external, GitHub-native)
2. GitHub Artifact Attestations for release artifacts
3. Documented provenance verification commands

## 3. Optional Set
- OpenSSF Best Practices Badge (self-attested program, externally visible)

## 4. NuGet-Relevant Evidence
All commands are intended to run from the repository root.
- Verify package signatures:
```bash
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
dotnet nuget verify "$NUPKG"
```
- Verify build provenance attestation:
```bash
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
```

## 5. Acceptance Criteria
- Scorecard workflow runs and stores artifacts.
- Release workflow emits attestations for nupkg artifacts.
- Verification commands are documented and reproducible.

## 6. Code Scanning Mode Constraint
This repository uses GitHub Code Scanning Advanced Setup (workflow-based CodeQL) and requires that GitHub Code Scanning
Default Setup remains disabled (state: `not-configured`).
When Default Setup is enabled, additional advanced CodeQL workflow uploads can be rejected by GitHub.
Operational rules for this repo:
- Keep Default Setup disabled for CodeQL (see `docs/security/110_CODEQL_DEFAULT_SETUP_GUARDRAIL.MD`).
- Treat Default-Setup drift as a fail-closed blocker and remediate before merging/releasing.
- Non-CodeQL SARIF uploads (for example OpenSSF Scorecard SARIF) are permitted.

## 7. OpenSSF Scorecard Constraint for Third-Party GitHub Actions
- Score values shown by `actions/dependency-review-action` for reused GitHub Actions are computed from the upstream action repositories (not from this repository).
- This repository can pin SHAs and choose actions, but cannot directly increase upstream Scorecard values.
- Operational handling:
  - PR comments do not include the OpenSSF Scorecard table (`show-openssf-scorecard: false`).
  - Warning threshold remains defined (`warn-on-openssf-scorecard-level: 9`) for machine-readable policy intent.
- Repro command for upstream scores:
```bash
curl -fsSL "https://api.securityscorecards.dev/projects/github.com/actions/checkout" | jq -r '.date,.score,.repo.name'
```
