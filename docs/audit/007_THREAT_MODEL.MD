<!-- LANG_SWITCH:BEGIN -->
[DE](007_THREAT_MODEL.MD) | [EN](107_THREAT_MODEL.MD)
<!-- LANG_SWITCH:END -->

# Threat Model

## 1. Zweck und Scope
In scope:
- Library-Verhalten in `src/FileTypeDetection/*`
- CI/CD und Release-Kontrollen in `.github/workflows/*`
- Security Reporting und Evidence-Lifecycle in `SECURITY.md` und `docs/audit/*`

Out of scope:
- Downstream Deployment-Hardening von konsumierenden Systemen
- Host-Level-Kontrollen ausserhalb dieses Repositories

## 2. Assets
- A1: Untrusted File-Payloads, die in Detection-/Extraction-Pfade gelangen
- A2: Extrahierte Archive-Entries im Memory
- A3: Hash-/Evidence-Artefakte fuer Integritaets-Assertions
- A4: Erzeugtes NuGet-Paket (`.nupkg`) und dessen Provenance
- A5: Vulnerability-Reports und Status der koordinierten Offenlegung

## 3. Trust Boundaries
- B1: Caller-Input -> Detector/Archive-Internals (`src/FileTypeDetection/FileTypeDetector.vb`)
- B2: Repository-Source -> CI-Execution (`.github/workflows/ci.yml`)
- B3: CI-erzeugtes Paket -> NuGet Publication (`.github/workflows/release.yml`)
- B4: Externer Reporter -> Maintainer-Triage (GitHub Private Vulnerability Reporting)

## 4. Attacker Capabilities
- C1: Crafted Files/Archives einspeisen, um unsafe Parsing/Extraction-Logik zu triggern
- C2: Path Traversal oder Resource Exhaustion ueber Archive-Payloads versuchen
- C3: Supply-Chain-Tampering zwischen Build und Publication versuchen
- C4: Disclosure-Abuse ueber oeffentliche Kanaele vor Fix-Readiness versuchen

## 5. Threat Scenarios und Kontrollen
- T1: Archive Traversal (zip-slip style path escape)
  - Impact: Write/Read ausserhalb des intended Scope waehrend der Extraktion.
  - Current controls: fail-closed Archive-Safety-Gates in Archive-Internals und Detector-Pfaden; Extraction-APIs sind auf sichere Flows eingeschraenkt.
  - Evidence: `tests/FileTypeDetectionLib.Tests/*` plus CI-Job `tests-bdd-coverage` in `.github/workflows/ci.yml`.
- T2: Decompression/Resource Exhaustion
  - Impact: Memory/CPU Pressure, moeglicher Denial-of-Service.
  - Current controls: guarded Archive-Processing-Pfade und bounded Validation/Extraction-Flows.
  - Evidence: Source-Pfade unter `src/FileTypeDetection/Infrastructure/*` und CI Execution Traces.
- T3: Release-Artifact Provenance Tampering
  - Impact: Consumer installieren untrusted Package-Build-Output.
  - Current controls: OIDC Trusted Publishing und Provenance Attestation in `.github/workflows/release.yml`.
  - Evidence: Release-Workflow Step `actions/attest-build-provenance@v2` und `gh attestation verify` Output-Artefakt.
- T4: Vulnerable Dependency Ingress
  - Impact: bekannte vulnerable transitive Komponenten im shipped Package.
  - Current controls: CI-Gate `security-nuget` (`dotnet list ... --vulnerable --include-transitive`) mit High/Critical fail behavior.
  - Evidence: `tools/ci/bin/run.sh` (`run_security_nuget`) und CI-Artefakte `artifacts/ci/security-nuget/*`.
- T5: Unkoordinierte Public Disclosure vor Patch
  - Impact: Exploitability-Window erhoeht sich, bevor Mitigation verfuegbar ist.
  - Current controls: Private Vulnerability Reporting Path und Coordinated Disclosure Prozess (wie in `SECURITY.md` deklariert).
  - Evidence: `tools/audit/verify-security-claims.sh` Claim-Checks und `docs/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD`.

## 6. Annahmen
- Maintainer-Access-Controls und Repo-Permissions bleiben korrekt konfiguriert.
- GitHub-hosted Security-Features bleiben fuer das Repository verfuegbar.
- Consumer setzen ihre eigenen Runtime-Hardening-Kontrollen um; dieses Repository kann sie nicht erzwingen.

## 7. Residual Risks
- R1: Runtime Denial-of-Service Risk kann fuer hostile Inputs nur reduziert, nicht eliminiert werden.
- R2: Organisatorische Response-Speed haengt von Maintainer-Verfuegbarkeit ab.
- R3: Third-Party-Platform-Outages koennen Triage/Release-Aktionen verzoegern.

## 8. Reproduzierbare Verifikation
Alle Kommandos sind fuer Ausfuehrung im Repository-Root gedacht.
```bash
bash tools/audit/verify-security-claims.sh
bash tools/ci/bin/run.sh security-nuget
NUPKG="$(find artifacts/nuget -maxdepth 1 -type f -name '*.nupkg' | head -n 1)"
test -n "$NUPKG"
gh attestation verify "$NUPKG" --repo tomtastisch/FileClassifier
```
