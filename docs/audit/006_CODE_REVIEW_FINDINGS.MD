<!-- LANG_SWITCH:BEGIN -->
[DE](006_CODE_REVIEW_FINDINGS.MD) | [EN](106_CODE_REVIEW_FINDINGS.MD)
<!-- LANG_SWITCH:END -->

# Code-Review Findings

## Zweck und Scope
Wave-1 Baseline-Findings aus JSON-Analyseartefakten.

## Aktueller Stand
Package A (Cluster 7C) hat generische Entry-Point-Catches fuer die gescopten Files entfernt (merged 2026-02-15).
Package B (Cluster 7C) reduziert duplizierte Archive/Stream-Guard-Fragmente in `Infrastructure/*` bei unveraendertem Verhalten.
Evidence-backed Kandidaten werden getrackt in:
- `artifacts/audit/dead_code_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `artifacts/audit/hardening_candidates.json`
- `artifacts/ci/code-analysis-evidence/result.json`

## Baseline-Findings (Wave 1)
1. Generische `Catch ex As Exception` Nutzung wurde aus den Package-A Entry-Point-Files entfernt (`FileTypeDetector`, `FileMaterializer`, `EvidenceHashing`).
   Verbleibende broad catches benoetigen Follow-up in Infrastructure-Internals (Package B/C Scope).
   - Beispiele:
     - `src/FileTypeDetection/Infrastructure/CoreInternals.vb:58`
     - `src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb:70`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:99`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:201`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:546`
   - Evidence: `artifacts/audit/hardening_candidates.json`

2. Wiederholte Long-Line-Patterns deuten auf potenzielle Guard-/Path-Handling-Duplikation in Archive- und Detector-Internals hin.
   - Beispiele:
     - `src/FileTypeDetection/Infrastructure/CoreInternals.vb:66`
     - `src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb:29`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:109`
   - Evidence: `artifacts/audit/redundancy_candidates.json`
   - Status: Package B konsolidiert wiederholte Stream-Guards via `StreamGuard` (SSOT) und ersetzt duplizierte `stream.CanRead`/Rewind-Patterns in den gescopten `Infrastructure/*` Files.

3. Dead-Code-Kandidaten-Scan lieferte unter der implementierten Heuristik aktuell keine low-reference Source-Declarations in `src/`.
   - Evidence: `artifacts/audit/dead_code_candidates.json`

4. `DetectDetailed(...).ReasonCode` nutzte einen generischen Bucket `"Exception"` fuer unterschiedliche Exception-Klassen (IO, Security, Argument, etc.).
   - Impact: reduzierte Auditierbarkeit von fail-closed Pfaden (Root-Causes waren im Ergebnis nicht unterscheidbar).
   - Status: Package C mappt File-Access-Exceptions auf deterministische, typisierte Reason-Codes.

## Repro-Evidence-Kommandos
Alle Kommandos sind fuer Ausfuehrung im Repository-Root gedacht.
```bash
bash tools/audit/verify-code-analysis-evidence.sh
jq '.candidates | length' artifacts/audit/hardening_candidates.json
jq '.candidates | length' artifacts/audit/redundancy_candidates.json
rg -n "Catch\\s+ex\\s+As\\s+Exception" src/FileTypeDetection/FileTypeDetector.vb src/FileTypeDetection/FileMaterializer.vb src/FileTypeDetection/EvidenceHashing.vb
rg -n "If\\s+stream\\s+Is\\s+Nothing\\s+OrElse\\s+Not\\s+stream\\.CanRead" src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb src/FileTypeDetection/Infrastructure/ArchiveInternals.vb
rg -n "If\\s+stream\\.CanSeek\\s+Then\\s+stream\\.Position\\s*=\\s*0" src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb src/FileTypeDetection/Infrastructure/ArchiveInternals.vb
```

## Findings-Format (fuer die naechsten PRs)
- ID
- file + line
- reproduzierbares Evidence-Kommando
- impact/risk
- proposed change
- required regression tests

## Prioritaets-Policy
- P1: Security-Korrektheit oder Behavioral-Risk
- P2: wahrscheinlich dead/redundant code mit mittlerer Confidence
- P3: Readability/Perf Opportunities mit geringem Risiko
