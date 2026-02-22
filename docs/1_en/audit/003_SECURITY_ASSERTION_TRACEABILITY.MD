<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/audit/003_SECURITY_ASSERTION_TRACEABILITY.MD) | [EN](003_SECURITY_ASSERTION_TRACEABILITY.MD)
<!-- LANG_SWITCH:END -->

# Security Assertion Traceability

## Scope
Mapping of claims in `SECURITY.md` to evidence sources and verification commands.

| Claim ID | SECURITY Anchor | Claim Summary | Evidence Source | Verification Command | Pass Criteria | Blocker |
|---|---|---|---|---|---|---|
| SEC-CLAIM-001 | 2. Unterstuetzte Versionen | Security support is tied to major 6 | `src/FileTypeDetection/FileTypeDetectionLib.vbproj` | `sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' src/FileTypeDetection/FileTypeDetectionLib.vbproj` | Version major is `6` | yes |
| SEC-CLAIM-002 | 3. Meldung | Private vulnerability reporting path exists | GitHub API `private-vulnerability-reporting` | `gh api "repos/$REPO/private-vulnerability-reporting"` | `.enabled == true` | yes |
| SEC-CLAIM-003 | 9. Nachweisbarkeit | Dependabot security updates enabled | GitHub API (`security_and_analysis` or `automated-security-fixes`) | `gh api "repos/$REPO" --jq '.security_and_analysis.dependabot_security_updates.status' || gh api "repos/$REPO/automated-security-fixes" --jq '.enabled'` | `enabled` or `true` | yes |
| SEC-CLAIM-004 | 9. Nachweisbarkeit | Secret scanning enabled | GitHub API (`security_and_analysis` or Secret-Scanning-Alerts Endpoint) | `gh api "repos/$REPO" --jq '.security_and_analysis.secret_scanning.status' || gh api "repos/$REPO/secret-scanning/alerts?per_page=1"` | `enabled` or endpoint reachable | yes |
| SEC-CLAIM-005 | 9. Nachweisbarkeit | Branch protection required contexts fixed | GitHub API branch rules (primary), branch protection (fallback) | `gh api "repos/$REPO/rules/branches/main" || gh api "repos/$REPO/branches/main/protection"` | expected context list present | yes |
| SEC-CLAIM-006 | 9. Nachweisbarkeit | `security-nuget` CI gate exists | `.github/workflows/ci.yml` | `rg -n "security-nuget|run\.sh security-nuget" .github/workflows/ci.yml` | gate + runner call both exist | yes |
| SEC-CLAIM-007 | 9. Nachweisbarkeit | OIDC trusted publishing configured | `.github/workflows/release.yml` | `rg -n "NuGet/login@v1|assert OIDC temp key present" .github/workflows/release.yml` | both markers present | yes |
| SEC-CLAIM-008 | 5. SLA | response target is 5 business days | policy declaration | manual policy review + incident log review | maintained process evidence exists | report-only |
| SEC-CLAIM-009 | 6. Safe Harbor | safe-harbor constraints are documented | `SECURITY.md` + runbook | `rg -n "Safe Harbor|gutglaeubige" SECURITY.md` | section present + runbook aligns | report-only |
| SEC-CLAIM-010 | 7. Koordinierte Offenlegung | coordinated disclosure process exists | runbook + advisory usage | review `docs/audit/108_INCIDENT_RESPONSE_RUNBOOK.MD` | workflow documented and actionable | report-only |
| SEC-CLAIM-011 | 3. Meldung | public issue tracker is not a primary disclosure channel | `SECURITY.md` policy text | `rg -n "nicht.*oeffentliche Issues|Nicht unterstuetzte" SECURITY.md` | policy text exists | report-only |
| SEC-CLAIM-012 | 4. Erforderliche Angaben | minimum report fields are explicitly listed | `SECURITY.md` section 4 | `rg -n "Erforderliche Angaben|Reproduktionsschritte|Impact" SECURITY.md` | section and required bullets exist | report-only |
| SEC-CLAIM-013 | 9. Nachweisbarkeit | local minimum evidence commands are executable | build/test + claim script | `dotnet build FileClassifier.sln -v minimal && dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal && bash tools/audit/verify-security-claims.sh` | all commands exit 0 | yes |
| SEC-CLAIM-014 | 9. Nachweisbarkeit | optional extended check IDs are available | CI runner implementation | `rg -n "tests-bdd-coverage|api-contract|pack|consumer-smoke|package-backed-tests" tools/ci/bin/run.sh` | all IDs resolvable | yes |
| SEC-CLAIM-015 | 1. Zweck und Geltungsbereich | ISO/IEC 29147 and 30111 orientation is documented | policy + roadmap docs | `rg -n "29147|30111" SECURITY.md docs/audit/104_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD` | references present with non-certification language | report-only |
| SEC-CLAIM-016 | 9. Zertifizierungsgrenze | no formal product certification claim is made | `SECURITY.md` section 9 | `rg -n "keine.*Zertifizierung|kein.*Rechtsgutachten" SECURITY.md` | explicit non-claim present | yes |

## Full Coverage Note
Claims that are normative process statements (policy intent) are classified as `report-only` unless they can be turned into deterministic machine checks.

## CI Claim Mapping
The verification script uses `CI-SEC-CLAIM-*` rule IDs. Mapping to normative claims:
- `CI-SEC-CLAIM-001` -> `SEC-CLAIM-002` (repository/reporting context resolvable)
- `CI-SEC-CLAIM-002` -> `SEC-CLAIM-001` (supported major version claim)
- `CI-SEC-CLAIM-003` -> `SEC-CLAIM-006` (`security-nuget` CI gate exists)
- `CI-SEC-CLAIM-004` -> `SEC-CLAIM-007` (OIDC trusted publishing markers)
- `CI-SEC-CLAIM-005` -> `SEC-CLAIM-003` (Dependabot security updates enabled/equivalent endpoint)
- `CI-SEC-CLAIM-006` -> `SEC-CLAIM-004` (secret scanning enabled/equivalent endpoint)
- `CI-SEC-CLAIM-007` -> `SEC-CLAIM-002` (private vulnerability reporting enabled)
- `CI-SEC-CLAIM-008` -> `SEC-CLAIM-005` (required status checks baseline)
