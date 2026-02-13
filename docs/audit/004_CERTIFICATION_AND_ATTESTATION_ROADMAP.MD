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
- Verify package signatures:
```bash
dotnet nuget verify artifacts/nuget/*.nupkg
```
- Verify build provenance attestation:
```bash
gh attestation verify artifacts/nuget/*.nupkg --repo tomtastisch/FileClassifier
```

## 5. Acceptance Criteria
- Scorecard workflow runs and stores artifacts.
- Release workflow emits attestations for nupkg artifacts.
- Verification commands are documented and reproducible.

## 6. Code Scanning Mode Constraint
This repository currently uses GitHub Code Scanning Default Setup (state: `configured`).
When Default Setup is enabled, additional advanced CodeQL workflow uploads can be rejected by GitHub.
Operational rule for this repo:
- Keep Default Setup active for CodeQL until an explicit migration decision is made.
- Do not run parallel advanced CodeQL workflow uploads in the same repository state.
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
