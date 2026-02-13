# Threat Model

## Assets
- File content payloads
- Archive extraction outputs
- Hash evidence reports
- Release artifacts (nupkg)

## Trust Boundaries
- Untrusted input files and archives
- CI/CD environment and GitHub-hosted runners
- NuGet publication boundary

## Primary Threats
- archive traversal / zip-slip vectors
- decompression bomb style payloads
- tampered release artifact provenance
- insecure disclosure handling of vulnerabilities

## Current Mitigations
- fail-closed archive checks in library logic
- CI `security-nuget` gate
- OIDC trusted publishing
- private vulnerability reporting process

## Residual Risks
- operational controls are deployer responsibility
- policy-process claims need periodic operational evidence refresh
