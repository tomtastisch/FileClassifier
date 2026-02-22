<!-- LANG_SWITCH:BEGIN -->
[DE](009_SUPPLY_CHAIN_BASELINE.MD) | [EN](../../1_en/audit/009_SUPPLY_CHAIN_BASELINE.MD)
<!-- LANG_SWITCH:END -->

# Supply-Chain-Basislinie

## 1. Ziel
Minimum an reproduzierbaren Kontrollen für Source-to-Package-Integrität in diesem Repository definieren.

## 2. Kontroll-Basislinie
- S1 Source-Integrität:
  - Branch-Protections und Required Status Checks auf dem Default-Branch
  - deterministische CI-Gates (`preflight`, `build`, `security-nuget`, `summary`)
- S2 Build-Integrität:
  - `dotnet restore --locked-mode` in CI/Release-Pfaden
  - fail-closed Policy-Checks in `tools/ci/bin/run.sh`
- S3 Release-Integrität:
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
  - `artifacts/nuget/attestation-verify.txt` (wenn der Release-Workflow läuft)
- E5 Dependency-Canary-Nachweise:
  - `.github/workflows/dependency-canary.yml`
  - Workflow-Artefakte `canary-*` mit Qodana-Contract-Output (`artifacts/ci/qodana/`)
  - Laufhistorie: https://github.com/tomtastisch/FileClassifier/actions/workflows/dependency-canary.yml

## 4. Verifikationskommandos
Alle Kommandos sind für Ausführung im Repository-Root gedacht.
```bash
bash tools/ci/bin/run.sh security-nuget
bash tools/audit/verify-security-claims.sh
bash tools/audit/verify-code-analysis-evidence.sh
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
dotnet nuget verify "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
# Optionaler Canary-Nachweis:
gh run list --workflow dependency-canary.yml --limit 5
```

## 5. Operative Kadenz
- Jede PR: CI, Security-Claims und Code-Analysis Evidence Workflows
- Jeder Release: Attestation-Generierung und Verifikation
- Regelmäßiger Review: Baseline-Dokus aktualisieren, wenn sich Controls oder Workflows ändern

## 6. Grenzen und Limits
- Diese Basislinie liefert Assurance-Nachweise, keine formale Third-Party-Zertifizierung.
- Downstream Runtime-Hardening bleibt Verantwortung von Deployern/Operatoren.

## 7. Dependency-Strategie für netstandard2.0-Transitives
- D1 Kontrollierte Pinning-Strategie:
  - `System.Text.Encoding.CodePages` wird explizit als direkte Abhängigkeit gepinnt (SSOT: `Directory.Packages.props`), damit SharpCompress-bezogene Encoding-Pfade nicht von impliziten Transitives abhängen.
- D2 Dokumentierte Ausnahme (Meta-Paket):
  - `Microsoft.NETCore.Platforms` bleibt als `netstandard2.0`-Transitiv aus `NETStandard.Library` sichtbar.
  - Diese Abweichung wird aktuell akzeptiert, da sie aus dem TFM-Meta-Paket stammt und nicht als eigenständiger Runtime-Upgrade-Pfad für das Produkt genutzt wird.
  - Security-Gate bleibt fail-closed: jede neue High/Critical-Vulnerability in diesem Pfad bleibt blocker.

Verifikationskommandos:
```bash
dotnet list FileClassifier.sln package --outdated --include-transitive
dotnet list FileClassifier.sln package --vulnerable --include-transitive
```
