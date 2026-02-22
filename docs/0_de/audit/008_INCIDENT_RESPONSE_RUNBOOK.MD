<!-- LANG_SWITCH:BEGIN -->
[DE](008_INCIDENT_RESPONSE_RUNBOOK.MD) | [EN](../../1_en/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD)
<!-- LANG_SWITCH:END -->

# Incident-Response-Runbook

## 1. Trigger Events
- Private Vulnerability Report via GitHub Security Reporting
- CI-Security-Gate-Failure mit exploitablem Impact (`security-nuget`, Provenance-Verifikation oder aequivalent)
- glaubwuerdiger externer Report zu aktiver Exploitation

## 2. Rollen
- Incident Owner: Repository-Maintainer on duty
- Technical Triage: Maintainer mit Code-Area-Ownership
- Release Owner: Maintainer mit Release-Permissions
- Communications Owner: Maintainer, der Disclosure/Advisory-Text koordiniert

## 3. SLA und Zeitziele
- Intake-Acknowledgement: innerhalb von 5 Geschaeftstagen (gemaess `SECURITY.md`)
- Triage-Decision: so frueh wie moeglich nach bestaetigter Reproduzierbarkeit
- Fix- und Disclosure-Timeline: risiko- und komplexitaetsbasiert, koordiniert mit Reporter

## 4. Severity-Modell (operational)
- Sev-1: bestaetigte kritische Exploitability oder aktive Exploitation
- Sev-2: hoher Impact mit realistischem Exploit-Pfad
- Sev-3: mittel/niedriger Impact oder schwer ausnutzbare Bedingungen

## 5. Vorgehen
1. Intake
- Report-Channel bestaetigen und Duplicate/New-Case klassifizieren.
- Interne Tracking-Notiz mit Timestamp und Reporter-Kontext erstellen.

2. Triage
- Reproduktion in isolierter Umgebung mit minimalem PoC.
- Betroffene Versionen und Impact-Surface bestimmen.

3. Containment
- Temporäre Mitigations (Config/Workaround/Doku-Guardrails) definieren, falls Fix nicht unmittelbar ist.
- Kein Public-Issue-Disclosure vor abgestimmtem, koordiniertem Plan.

4. Remediation
- Minimal-Risk-Patch in fokussierten Commit(s) implementieren.
- Regression-Tests fuer den Exploit-Pfad ergaenzen/anpassen.

5. Verifikation
- Security-relevante Checks ausfuehren und exakte Command-Outputs dokumentieren:
  Alle Kommandos sind fuer Ausfuehrung im Repository-Root gedacht.
```bash
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/ci/bin/run.sh security-nuget
bash tools/audit/verify-security-claims.sh
```

6. Release und Disclosure
- Advisory/Release Notes mit affected/fixed versions vorbereiten.
- Koordinierte Offenlegung veroeffentlichen, sobald Fix-Verfuegbarkeit bestaetigt ist.

7. Abschluss
- Post-Incident Summary, Root Cause und Follow-ups dokumentieren.
- Fix-Commit(s), Tests und Evidence-Artefakte verlinken.

## 6. Evidence-Checkliste
- Timeline mit UTC Timestamps
- Severity-Assignment inkl. Begruendung
- Affected Version Range und Fixed Version
- Repro-Schritte und Verifikationskommandos
- Commit-SHAs und PR-Referenzen
- Advisory/Disclosure-Referenz

## 7. Evidence Storage
- CI-Artefakte: `artifacts/ci/*`
- Claim-Evidence: `artifacts/ci/security-claims-evidence/*`
- Package/Release-Evidence: `artifacts/nuget/*` (wenn verfuegbar)

## 8. Non-Goals
- Dieses Runbook ist keine Rechtsberatung.
- Dieses Runbook behauptet fuer sich genommen keine formale Zertifizierung oder regulatorische Abdeckung.
