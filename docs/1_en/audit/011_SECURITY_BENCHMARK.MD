<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/audit/011_SECURITY_BENCHMARK.MD) | [EN](011_SECURITY_BENCHMARK.MD)
<!-- LANG_SWITCH:END -->

# Security Policy Benchmark (as of 2026-02-13)

## 1. Goal and Scope
Comparison of the security-policy maturity of `tomtastisch/FileClassifier` (PR branch `tomtastisch-patch-1`) with common .NET open-source repositories based on verifiable GitHub and repository facts.
This benchmark is a snapshot before merge into `main` (as of 2026-02-13).

Compared repositories:
- `tomtastisch/FileClassifier`
- `dotnet/runtime`
- `dotnet/aspnetcore`
- `JamesNK/Newtonsoft.Json`
- `DapperLib/Dapper`
- `serilog/serilog`
- `AutoMapper/AutoMapper`
- `App-vNext/Polly`
- `FluentValidation/FluentValidation`
- `xunit/xunit`
- `NLog/NLog`

## 2. Method (facts only)
Collected via GitHub API and local file inspection:
- presence of `SECURITY.md` (FileClassifier: repo-root `SECURITY.md`; other repos may alternatively use `.github/SECURITY.md`)
- status `private-vulnerability-reporting`
- visible `security_and_analysis` fields (`dependabot_security_updates`, `secret_scanning`)
- presence of `.github/dependabot.yml`
- presence of workflow files with `codeql` in the file name
- content markers in `SECURITY.md`: support table, reporting, SLA time targets, safe harbor, ISO/IEC 29147/30111, coordinated disclosure

## 3. Result A - Platform/repo signals
| Repository | SECURITY.md | Private Vulnerability Reporting | Dependabot Security Updates | Secret Scanning | CodeQL workflow file | dependabot.yml |
|---|---|---|---|---|---|---|
| tomtastisch/FileClassifier | no (snapshot before merge into `main`) | true | enabled | enabled | no | no |
| dotnet/runtime | yes | false_or_unavailable | unknown | unknown | no | yes |
| dotnet/aspnetcore | yes | false_or_unavailable | unknown | unknown | no | yes |
| JamesNK/Newtonsoft.Json | no | false_or_unavailable | unknown | unknown | yes | no |
| DapperLib/Dapper | no | false_or_unavailable | unknown | unknown | no | no |
| serilog/serilog | yes | true | unknown | unknown | no | no |
| AutoMapper/AutoMapper | no | true | unknown | unknown | no | no |
| App-vNext/Polly | yes | true | unknown | unknown | no | yes |
| FluentValidation/FluentValidation | no | false_or_unavailable | unknown | unknown | no | no |
| xunit/xunit | no | false_or_unavailable | unknown | unknown | no | no |
| NLog/NLog | yes | true | unknown | unknown | no | yes |

Notes:
- `unknown` means: the field is not published or not visible via the GitHub API for the external repo.
- `false_or_unavailable` means: the API endpoint does not show an active configuration.

## 4. Result B - SECURITY.md content comparison
| Repository | Supported versions | Reporting section | SLA time targets | Safe harbor | ISO/IEC 29147/30111 | Coordinated disclosure |
|---|---|---|---|---|---|---|
| dotnet/runtime | yes | yes | yes | no | no | no |
| dotnet/aspnetcore | yes | yes | yes | no | no | no |
| serilog/serilog | yes | yes | no | no | no | no |
| App-vNext/Polly | no | yes | yes | no | no | no |
| NLog/NLog | yes | yes | no | no | no | no |
| tomtastisch/FileClassifier (local, PR) | yes | yes | yes | yes | yes | yes |

## 5. Interpretation (fact-oriented)
- The new `SECURITY.md` for FileClassifier is more comprehensive in content than the comparison files (notably safe harbor, ISO/IEC orientation, coordinated disclosure).
- Snapshot note: at the time of collection, the file was on PR branch `tomtastisch-patch-1` and not yet in `main`.
- Repository-side security mechanisms for FileClassifier are verifiably active:
  - private vulnerability reporting (`enabled=true`)
  - Dependabot security updates (`enabled`)
  - secret scanning (`enabled`)
  - CI security gate `security-nuget` (verified locally with `pass`)

## 6. Remaining gaps for \"certification-adjacent\" readiness
These points are outside a single `SECURITY.md`, but are needed for robust audit/certification readiness:
- formal SDLC evidence (documented roles, approvals, change management)
- threat modeling artifacts with regular updates
- incident response runbooks with exercises and evidence
- SBOM/provenance strategy for releases (including retention)
- formal evidence for production operational controls by the operator

## 7. Reproducibility
Used commands (excerpt):
All commands are intended to run from the repository root.
```bash
REPO="<owner>/<repo>"
gh api "repos/$REPO"
gh api "repos/$REPO/private-vulnerability-reporting"
gh api "repos/$REPO/contents/SECURITY.md"
gh api "repos/$REPO/contents/.github/SECURITY.md"
gh api "repos/$REPO/contents/.github/workflows"
gh api "repos/$REPO/contents/.github/dependabot.yml"
```

Local verification for FileClassifier:
```bash
python3 tools/check-docs.py
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/ci/bin/run.sh security-nuget
```
