# Code Review Findings

## Scope
Wave 1 baseline findings from JSON analysis artifacts.

## Current State
No high-confidence refactor was auto-applied in wave 1.
Evidence-backed candidates are tracked in:
- `artifacts/audit/dead_code_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `artifacts/audit/hardening_candidates.json`

## Baseline Findings (Wave 1)
1. `Catch ex As Exception` broad catches appear repeatedly in production code and should be reviewed for narrower exception types or enriched reason codes.
   - Examples:
     - `src/FileTypeDetection/FileTypeDetector.vb:77`
     - `src/FileTypeDetection/FileTypeDetector.vb:206`
     - `src/FileTypeDetection/FileTypeDetector.vb:296`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:99`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:546`
   - Evidence: `artifacts/audit/hardening_candidates.json`

2. Repeated long-line patterns indicate potential guard/path handling duplication across archive and detector internals.
   - Examples:
     - `src/FileTypeDetection/Infrastructure/CoreInternals.vb:66`
     - `src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb:29`
     - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb:109`
   - Evidence: `artifacts/audit/redundancy_candidates.json`

3. Dead-code candidate scan currently returned no low-reference source declarations in `src/` under the implemented heuristic.
   - Evidence: `artifacts/audit/dead_code_candidates.json`

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
