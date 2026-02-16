<!-- LANG_SWITCH:BEGIN -->
[DE](004_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD) | [EN](104_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD)
<!-- LANG_SWITCH:END -->

# Roadmap: Zertifizierung und Attestationen

## 1. Positionierung
Dieses Projekt unterscheidet:
- Assurance/Attestation-Evidence (technisch, reproduzierbar)
- Formale Third-Party-Zertifizierung (externer Audit, ausserhalb des aktuellen Scopes)

Dieses Repository macht keinen formalen Zertifizierungs-Claim.

## 2. Minimum Set (Wave 1)
1. OpenSSF Scorecard (kostenlos, extern, GitHub-nativ)
2. GitHub Artifact Attestations fuer Release-Artefakte
3. Dokumentierte Verifikationskommandos fuer Provenance/Attestationen

## 3. Optional Set
- OpenSSF Best Practices Badge (Self-Attested Programm, extern sichtbar)

## 4. NuGet-relevante Evidence
Alle Kommandos sind fuer Ausfuehrung im Repository-Root gedacht.
- Paket-Signaturen verifizieren:
```bash
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
dotnet nuget verify "$NUPKG"
```
- Build-Provenance Attestation verifizieren:
```bash
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
```

## 5. Akzeptanzkriterien
- Scorecard-Workflow laeuft und speichert Artefakte.
- Release-Workflow emittiert Attestationen fuer nupkg-Artefakte.
- Verifikationskommandos sind dokumentiert und reproduzierbar.

## 6. Code-Scanning-Mode-Constraint
Dieses Repository nutzt GitHub Code Scanning Advanced Setup (workflow-basierte CodeQL Analyse) und erfordert,
dass GitHub Code Scanning Default Setup deaktiviert bleibt (state: `not-configured`).
Wenn Default Setup aktiv ist, koennen Advanced-CodeQL SARIF Uploads von GitHub abgelehnt werden.

Operative Regeln fuer dieses Repo:
- Default Setup fuer CodeQL deaktiviert halten (siehe `docs/security/010_CODEQL_DEFAULT_SETUP_GUARDRAIL.MD`).
- Default-Setup-Drift als fail-closed Blocker behandeln und vor Merge/Release beheben.
- Nicht-CodeQL SARIF Uploads (z. B. OpenSSF Scorecard SARIF) sind zulaessig.

## 7. OpenSSF-Scorecard-Constraint fuer Third-Party GitHub Actions
- Score-Werte, die z. B. `actions/dependency-review-action` fuer wiederverwendete GitHub Actions zeigt, werden aus den Upstream-Action-Repositories berechnet (nicht aus diesem Repository).
- Dieses Repository kann SHA-Pinning durchsetzen und Actions auswaehlen, aber nicht direkt Upstream-Scorecard-Werte erhoehen.

Operatives Handling:
- PR-Kommentare enthalten keine OpenSSF Scorecard Tabelle (`show-openssf-scorecard: false`).
- Warning-Threshold bleibt definiert (`warn-on-openssf-scorecard-level: 9`) als machine-readable Policy-Intent.

Repro-Kommando fuer Upstream-Scores:
```bash
curl -fsSL "https://api.securityscorecards.dev/projects/github.com/actions/checkout" | jq -r '.date,.score,.repo.name'
```
