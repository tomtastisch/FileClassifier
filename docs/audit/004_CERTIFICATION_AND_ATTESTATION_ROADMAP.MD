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
dotnet nuget verify <package.nupkg>
```
- Verify build provenance attestation:
```bash
gh attestation verify <artifact> --repo <owner/repo>
```

## 5. Acceptance Criteria
- Scorecard workflow runs and stores artifacts.
- Release workflow emits attestations for nupkg artifacts.
- Verification commands are documented and reproducible.
