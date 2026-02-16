<!-- LANG_SWITCH:BEGIN -->
[DE](009_SUPPLY_CHAIN_BASELINE.MD) | [EN](109_SUPPLY_CHAIN_BASELINE.MD)
<!-- LANG_SWITCH:END -->

# Supply-Chain-Basislinie

## 1. Ziel
Minimum an reproduzierbaren Kontrollen fuer Source-to-Package-Integritaet in diesem Repository definieren.

## 2. Kontroll-Basislinie
- S1 Source-Integritaet:
  - Branch-Protections und Required Status Checks auf dem Default-Branch
  - deterministische CI-Gates (`preflight`, `build`, `security-nuget`, `summary`)
- S2 Build-Integritaet:
  - `dotnet restore --locked-mode` in CI/Release-Pfaden
  - fail-closed Policy-Checks in `tools/ci/bin/run.sh`
- S3 Release-Integritaet:
  - OIDC Trusted Publishing in `.github/workflows/release.yml`
  - Provenance Attestation via `actions/attest-build-provenance@v2`
- S4 Vulnerability Hygiene:
  - NuGet Vulnerability Gate (`security-nuget`)
  - Security-Claims-Verifikation (`security-claims-evidence`)

## 3. Nachweis-Abbildung
- E1 CI-Workflow-Nachweise:
  - `.github/workflows/ci.yml`
  - `artifacts/ci/*`
- E2 Security-Claim-Nachweise:
  - `.github/workflows/security-claims-evidence.yml`
  - `artifacts/ci/security-claims-evidence/result.json`
- E3 Code-Analysis-Nachweise:
  - `.github/workflows/code-analysis-evidence.yml`
  - `artifacts/ci/code-analysis-evidence/result.json`
- E4 Release/Provenance-Nachweise:
  - `.github/workflows/release.yml`
  - `artifacts/nuget/attestation-verify.txt` (wenn der Release-Workflow laeuft)

## 4. Verifikationskommandos
Alle Kommandos sind fuer Ausfuehrung im Repository-Root gedacht.
```bash
bash tools/ci/bin/run.sh security-nuget
bash tools/audit/verify-security-claims.sh
bash tools/audit/verify-code-analysis-evidence.sh
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
dotnet nuget verify "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
```

## 5. Operative Kadenz
- Jede PR: CI, Security-Claims und Code-Analysis Evidence Workflows
- Jeder Release: Attestation-Generierung und Verifikation
- Regelmaessiger Review: Baseline-Dokus aktualisieren, wenn sich Controls oder Workflows aendern

## 6. Grenzen und Limits
- Diese Basislinie liefert Assurance-Nachweise, keine formale Third-Party-Zertifizierung.
- Downstream Runtime-Hardening bleibt Verantwortung von Deployern/Operatoren.
