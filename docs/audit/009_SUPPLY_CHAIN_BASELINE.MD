# Supply Chain Baseline

## Build and Publish Controls
- locked-mode restore in CI/release paths
- deterministic versioning gates
- OIDC trusted publishing for NuGet
- package verification commands documented

## Evidence Sources
- CI artifacts under `artifacts/ci/*`
- release workflow logs
- attestation verification outputs

## Verification Commands
```bash
bash tools/ci/bin/run.sh security-nuget
dotnet nuget verify <package.nupkg>
gh attestation verify <artifact> --repo <owner/repo>
```
