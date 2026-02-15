# Code Review Findings

## Scope
Wave 1 baseline findings from JSON analysis artifacts.

## Current State
Package A (Cluster 7C) removed generic entry-point catches for the scoped files (merged 2026-02-15).
Package B (Cluster 7C) reduces duplicated archive/stream guard fragments in `Infrastructure/*` while preserving behavior.
Evidence-backed candidates are tracked in:
- `artifacts/audit/dead_code_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `artifacts/audit/hardening_candidates.json`
- `artifacts/ci/code-analysis-evidence/result.json`

## Baseline Findings (Wave 1)
1. Generic `Catch ex As Exception` usage was removed from Package A entry-point files (`FileTypeDetector`, `FileMaterializer`, `EvidenceHashing`).
   Remaining broad catches still require follow-up in infrastructure internals (Package B/C scope).
   - Examples:
     - `src/FileTypeDetection/Infrastructure/CoreInternals.vb:58`
     - `src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb:70`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:99`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:201`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:546`
   - Evidence: `artifacts/audit/hardening_candidates.json`

2. Repeated long-line patterns indicate potential guard/path handling duplication across archive and detector internals.
   - Examples:
     - `src/FileTypeDetection/Infrastructure/CoreInternals.vb:66`
     - `src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb:29`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:109`
   - Evidence: `artifacts/audit/redundancy_candidates.json`
   - Status: Package B consolidates repeated stream guards via `StreamGuard` (SSOT), replacing the duplicated `stream.CanRead` and rewind patterns in the scoped `Infrastructure/*` files.

3. Dead-code candidate scan currently returned no low-reference source declarations in `src/` under the implemented heuristic.
   - Evidence: `artifacts/audit/dead_code_candidates.json`

## Repro Evidence Commands
All commands are intended to run from the repository root.
```bash
bash tools/audit/verify-code-analysis-evidence.sh
jq '.candidates | length' artifacts/audit/hardening_candidates.json
jq '.candidates | length' artifacts/audit/redundancy_candidates.json
rg -n "Catch\\s+ex\\s+As\\s+Exception" src/FileTypeDetection/FileTypeDetector.vb src/FileTypeDetection/FileMaterializer.vb src/FileTypeDetection/EvidenceHashing.vb
rg -n "If\\s+stream\\s+Is\\s+Nothing\\s+OrElse\\s+Not\\s+stream\\.CanRead" src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb src/FileTypeDetection/Infrastructure/ArchiveInternals.vb
rg -n "If\\s+stream\\.CanSeek\\s+Then\\s+stream\\.Position\\s*=\\s*0" src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb src/FileTypeDetection/Infrastructure/ArchiveInternals.vb
```

## Findings Format (for next PRs)
- ID
- file + line
- reproducible evidence command
- impact/risk
- proposed change
- required regression tests

## Priority Policy
- P1: security correctness or behavioral risk
- P2: likely dead/redundant code with medium confidence
- P3: readability/perf opportunities with low risk
