<!-- LANG_SWITCH:BEGIN -->
[DE](008_INCIDENT_RESPONSE_RUNBOOK.MD) | [EN](108_INCIDENT_RESPONSE_RUNBOOK.MD)
<!-- LANG_SWITCH:END -->

# Incident-Response-Runbook

## 1. Trigger Events
- Private Vulnerability Report via GitHub Security Reporting
- CI-Security-Gate-Failure mit exploitablem Impact (`security-nuget`, Provenance-Verifikation oder äquivalent)
- glaubwürdiger externer Report zu aktiver Exploitation

## 2. Rollen
- Incident Owner: Repository-Maintainer on duty
- Technical Triage: Maintainer mit Code-Area-Ownership
- Release Owner: Maintainer mit Release-Permissions
- Communications Owner: Maintainer, der Disclosure/Advisory-Text koordiniert

## 3. SLA und Zeitziele
- Intake-Acknowledgement: innerhalb von 5 Geschäftstagen (gemäß `SECURITY.md`)
- Triage-Decision: so früh wie möglich nach bestätigter Reproduzierbarkeit
- Fix- und Disclosure-Timeline: risiko- und komplexitätsbasiert, koordiniert mit Reporter

## 4. Severity-Modell (operational)
- Sev-1: bestätigte kritische Exploitability oder aktive Exploitation
- Sev-2: hoher Impact mit realistischem Exploit-Pfad
- Sev-3: mittel/niedriger Impact oder schwer ausnutzbare Bedingungen

## 5. Vorgehen
1. Intake
- Report-Channel bestätigen und Duplicate/New-Case klassifizieren.
- Interne Tracking-Notiz mit Timestamp und Reporter-Kontext erstellen.

2. Triage
- Reproduktion in isolierter Umgebung mit minimalem PoC.
- Betroffene Versionen und Impact-Surface bestimmen.

3. Containment
- Temporäre Mitigations (Config/Workaround/Doku-Guardrails) definieren, falls Fix nicht unmittelbar ist.
- Kein Public-Issue-Disclosure vor abgestimmtem, koordiniertem Plan.

4. Remediation
- Minimal-Risk-Patch in fokussierten Commit(s) implementieren.
- Regression-Tests für den Exploit-Pfad ergänzen/anpassen.

5. Verifikation
- Security-relevante Checks ausführen und exakte Command-Outputs dokumentieren:
  Alle Kommandos sind für Ausführung im Repository-Root gedacht.
```bash
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/ci/bin/run.sh security-nuget
bash tools/audit/verify-security-claims.sh
```

6. Release und Disclosure
- Advisory/Release Notes mit affected/fixed versions vorbereiten.
- Koordinierte Offenlegung veröffentlichen, sobald Fix-Verfügbarkeit bestätigt ist.

7. Abschluss
- Post-Incident Summary, Root Cause und Follow-ups dokumentieren.
- Fix-Commit(s), Tests und Evidence-Artefakte verlinken.

## 6. Evidence-Checkliste
- Timeline mit UTC Timestamps
- Severity-Assignment inkl. Begründung
- Affected Version Range und Fixed Version
- Repro-Schritte und Verifikationskommandos
- Commit-SHAs und PR-Referenzen
- Advisory/Disclosure-Referenz

## 7. Evidence Storage
- CI-Artefakte: `artifacts/ci/*`
- Claim-Evidence: `artifacts/ci/security-claims-evidence/*`
- Package/Release-Evidence: `artifacts/nuget/*` (wenn verfügbar)

## 8. Non-Goals
- Dieses Runbook ist keine Rechtsberatung.
- Dieses Runbook behauptet für sich genommen keine formale Zertifizierung oder regulatorische Abdeckung.
