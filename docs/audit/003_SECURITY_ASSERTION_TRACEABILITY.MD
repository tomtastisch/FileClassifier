<!-- LANG_SWITCH:BEGIN -->
[DE](003_SECURITY_ASSERTION_TRACEABILITY.MD) | [EN](103_SECURITY_ASSERTION_TRACEABILITY.MD)
<!-- LANG_SWITCH:END -->

# Traceability: Security-Claims (SECURITY.md)

## Zweck und Scope
Mapping von Claims in `SECURITY.md` auf Evidence-Quellen und Verifikationskommandos.

| Claim ID | SECURITY-Anker | Claim-Zusammenfassung | Evidence-Quelle | Verifikationskommando | Pass-Kriterium | Blocker |
|---|---|---|---|---|---|---|
| SEC-CLAIM-001 | 2. Unterstuetzte Versionen | Security-Support ist an Major 5 gebunden | `src/FileTypeDetection/FileTypeDetectionLib.vbproj` | `sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' src/FileTypeDetection/FileTypeDetectionLib.vbproj` | Version-Major ist `5` | yes |
| SEC-CLAIM-002 | 3. Meldung | Private Vulnerability Reporting ist aktiv | GitHub API `private-vulnerability-reporting` | `gh api "repos/$REPO/private-vulnerability-reporting"` | `.enabled == true` | yes |
| SEC-CLAIM-003 | 9. Nachweisbarkeit | Dependabot Security Updates sind aktiv | GitHub API (`security_and_analysis` oder `automated-security-fixes`) | `gh api "repos/$REPO" --jq '.security_and_analysis.dependabot_security_updates.status' || gh api "repos/$REPO/automated-security-fixes" --jq '.enabled'` | `enabled` oder `true` | yes |
| SEC-CLAIM-004 | 9. Nachweisbarkeit | Secret Scanning ist aktiv | GitHub API (`security_and_analysis` oder Secret-Scanning-Alerts Endpoint) | `gh api "repos/$REPO" --jq '.security_and_analysis.secret_scanning.status' || gh api "repos/$REPO/secret-scanning/alerts?per_page=1"` | `enabled` oder Endpoint erfolgreich erreichbar | yes |
| SEC-CLAIM-005 | 9. Nachweisbarkeit | Required Status Checks/Contexts sind fixiert | GitHub API branch rules (primaer), branch protection (Fallback) | `gh api "repos/$REPO/rules/branches/main" || gh api "repos/$REPO/branches/main/protection"` | erwartete Context-Liste vorhanden | yes |
| SEC-CLAIM-006 | 9. Nachweisbarkeit | CI-Gate `security-nuget` existiert | `.github/workflows/ci.yml` | `rg -n "security-nuget|run\\.sh security-nuget" .github/workflows/ci.yml` | Gate + Runner-Call existieren | yes |
| SEC-CLAIM-007 | 9. Nachweisbarkeit | OIDC Trusted Publishing ist konfiguriert | `.github/workflows/release.yml` | `rg -n "NuGet/login@v1|assert OIDC temp key present" .github/workflows/release.yml` | beide Marker vorhanden | yes |
| SEC-CLAIM-008 | 5. SLA | Reaktionsziel sind 5 Geschaeftstage | Policy-Statement | manueller Policy-Review + Incident-Log-Review | gepflegte Prozess-Evidence existiert | report-only |
| SEC-CLAIM-009 | 6. Safe Harbor | Safe-Harbor Constraints sind dokumentiert | `SECURITY.md` + Runbook | `rg -n "Safe Harbor|gutglaeubige" SECURITY.md` | Section vorhanden + Runbook aligned | report-only |
| SEC-CLAIM-010 | 7. Koordinierte Offenlegung | Koordinierter Disclosure-Prozess existiert | Runbook + Advisory-Usage | Review `docs/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD` | Workflow dokumentiert und umsetzbar | report-only |
| SEC-CLAIM-011 | 3. Meldung | Public Issue Tracker ist kein primaerer Disclosure-Channel | `SECURITY.md` Policy-Text | `rg -n "nicht.*oeffentliche Issues|Nicht unterstuetzte" SECURITY.md` | Policy-Text vorhanden | report-only |
| SEC-CLAIM-012 | 4. Erforderliche Angaben | Minimum-Reportfelder sind explizit gelistet | `SECURITY.md` Section 4 | `rg -n "Erforderliche Angaben|Reproduktionsschritte|Impact" SECURITY.md` | Section + Required Bullets vorhanden | report-only |
| SEC-CLAIM-013 | 9. Nachweisbarkeit | Lokale Minimum-Evidence-Kommandos sind ausfuehrbar | Build/Test + Claim-Script | `dotnet build FileClassifier.sln -v minimal && dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal && bash tools/audit/verify-security-claims.sh` | alle Kommandos exit 0 | yes |
| SEC-CLAIM-014 | 9. Nachweisbarkeit | Optionale erweiterte Check-IDs sind verfuegbar | CI-Runner-Implementation | `rg -n "tests-bdd-coverage|api-contract|pack|consumer-smoke|package-backed-tests" tools/ci/bin/run.sh` | alle IDs resolvable | yes |
| SEC-CLAIM-015 | 1. Zweck/Geltungsbereich | ISO/IEC 29147 und 30111 Orientierung ist dokumentiert | Policy + Roadmap-Dokus | `rg -n "29147|30111" SECURITY.md docs/audit/004_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD` | Referenzen vorhanden (ohne Zertifizierungsclaim) | report-only |
| SEC-CLAIM-016 | 9. Zertifizierungsgrenze | Es wird keine formale Produkt-Zertifizierung behauptet | `SECURITY.md` Section 9 | `rg -n "keine.*Zertifizierung|kein.*Rechtsgutachten" SECURITY.md` | expliziter Non-Claim vorhanden | yes |

## Full-Coverage-Hinweis
Claims, die normative Prozess-Statements sind (Policy-Intent), werden als `report-only` klassifiziert, solange sie nicht in deterministische Machine-Checks ueberfuehrt werden koennen.

## CI-Claim-Mapping
Das Verifikationsscript verwendet `CI-SEC-CLAIM-*` Rule-IDs. Mapping auf normative Claims:
- `CI-SEC-CLAIM-001` -> `SEC-CLAIM-002` (Repository/Reporting-Context ist aufloesbar)
- `CI-SEC-CLAIM-002` -> `SEC-CLAIM-001` (Supported-Major-Version-Claim)
- `CI-SEC-CLAIM-003` -> `SEC-CLAIM-006` (CI-Gate `security-nuget` existiert)
- `CI-SEC-CLAIM-004` -> `SEC-CLAIM-007` (OIDC Trusted Publishing Marker)
- `CI-SEC-CLAIM-005` -> `SEC-CLAIM-003` (Dependabot Security Updates aktiv / aequivalenter Endpoint)
- `CI-SEC-CLAIM-006` -> `SEC-CLAIM-004` (Secret Scanning aktiv / aequivalenter Endpoint)
- `CI-SEC-CLAIM-007` -> `SEC-CLAIM-002` (Private Vulnerability Reporting aktiv)
- `CI-SEC-CLAIM-008` -> `SEC-CLAIM-005` (Required Status Checks Baseline)
