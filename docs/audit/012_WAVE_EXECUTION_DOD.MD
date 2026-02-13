# Wave Execution DoD Matrix

Stand: 2026-02-13
Scope: Finalisierung aller offenen Security-/Governance-Punkte nach Wave-Plan.

## Cluster 0 - Guardrails & Contract
- DoD-0.1: `tools/ci/schema/result.schema.json` validiert alle erzeugten `result.json` in CI ohne Sonderfall-Bypass.
- DoD-0.2: Für jeden blockierenden Check existiert ein eindeutiger Fail-Reason im Artefakt (`rule_id`, `message`, `evidence_paths`).

## Cluster 1 - SECURITY Truth Matrix
- DoD-1.1: Jede Aussage aus `SECURITY.md` hat eine `SEC-CLAIM-*`-Zuordnung in `docs/audit/003_SECURITY_ASSERTION_TRACEABILITY.MD`.
- DoD-1.2: Jede blocker-klassifizierte Aussage hat mindestens ein reproduzierbares Kommando mit `exit 0/!=0`-Kriterium.

## Cluster 2 - Evidence Engine
- DoD-2.1: `bash tools/audit/verify-security-claims.sh` erzeugt `raw.log`, `summary.md`, `result.json` schema-valide.
- DoD-2.2: API-Fehler sind in Reason-Codes klassifiziert (`auth|rate-limit|network|5xx|unknown`).

## Cluster 3 - CI Integration
- DoD-3.1: `security-claims-evidence` und `code-analysis-evidence` laufen auf `push` und `pull_request` gegen `main`.
- DoD-3.2: Blocker-Claims failen deterministisch; report-only Claims blockieren Merge nicht.

## Cluster 4 - Externe Audits/Attestations
- DoD-4.1: OpenSSF Scorecard Workflow erzeugt SARIF und lädt Artefakte hoch.
- DoD-4.2: Release Workflow erzeugt und verifiziert Build-Attestation (`gh attestation verify`).

## Cluster 5 - Sichtbarkeit Repo/NuGet
- DoD-5.1: Root-Nachweisdatei `SECURITY_ASSURANCE_INDEX.md` ist vorhanden und verlinkt alle Kern-Evidenzen.
- DoD-5.2: README enthält sichtbare Sektion `Security Assurance & Evidence` mit Audit-/Runbook-/Threat-Links.

## Cluster 6 - CodeQL / Scanning Quality
- DoD-6.1: Für jeden Main-Commit laufen `Analyze (actions|csharp|javascript-typescript|python)` erfolgreich.
- DoD-6.2: Kein paralleler inkompatibler CodeQL-Uploadpfad (Default-Setup konfliktfrei dokumentiert/angewendet).

## Cluster 7 - Deep Analysis (JSON-only)
- DoD-7.1: JSON-Artefakte `code_inventory|callgraph_inventory|dead_code_candidates|redundancy_candidates|hardening_candidates` werden erzeugt.
- DoD-7.2: Findings mit Datei+Zeile sind in `docs/audit/006_CODE_REVIEW_FINDINGS.MD` reproduzierbar referenziert.

## Cluster 8 - Governance Closure
- DoD-8.1: Threat Model, Incident Runbook, Supply-Chain Baseline sind vorhanden und gegenseitig konsistent.
- DoD-8.2: Dokumente referenzieren reale, bestehende Workflows/Artefakte ohne Platzhalter.

## Snapshot Warning Hardening (Dependency Review)
- DoD-SW.1: `dependency-review` verwendet `retry-on-snapshot-warnings: true`.
- DoD-SW.2: PR-Kommentar-Output erfolgt nur bei Failure (`comment-summary-in-pr: on-failure`), damit Warnhinweise nicht als Dauerrauschen erscheinen.

## Release Finalisierung
- DoD-R.1: Nach Merge ist Tag `v5.1.0` erstellt und Release-Pipeline erfolgreich abgeschlossen.
- DoD-R.2: `git version == nupkg version == nuget version` ist im Release-Artefakt nachweisbar.
