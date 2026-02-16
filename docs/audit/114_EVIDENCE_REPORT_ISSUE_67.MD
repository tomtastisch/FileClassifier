<!-- LANG_SWITCH:BEGIN -->
[DE](014_EVIDENCE_REPORT_ISSUE_67.MD) | [EN](114_EVIDENCE_REPORT_ISSUE_67.MD)
<!-- LANG_SWITCH:END -->

# Evidence Report — Issue #67 (Full Refactor & Production-Readiness Hardening)

## 1. Goal / Scope
This report is the central, reproducible evidence document for issue #67.
It includes:
- baseline snapshot (repo state, tool versions, executed checks)
- inventory (modules/files as machine-readable JSON)
- findings (prioritized, citable, with evidence source)
- PR mapping (planned/implemented PR packages)

Not included (non-goals):
- no change to `SECURITY.md` (frozen).
- no commit-based artifacts under `artifacts/` (produced reproducibly and `.gitignore`d).

## 2. Baseline Snapshot (Phase 0)
Snapshot commit:
- commit: `0c78a9664a6f4abc7fd9d25da92d5c5c741aeb92`
- date (commit): `2026-02-15 16:18:30 +0100`
- branch (local): `codex/docs/issue-67-baseline-snapshot`

Tooling (local, baseline):
- dotnet SDK: `10.0.102` (see `global.json`)
- Node: `v25.5.0`
- Python: `3.14.2`
- jq: `jq-1.8.1`
- ripgrep: `15.1.0`
- gh: `2.86.0`

ASSUMPTION:
- Tool versions can differ locally vs CI (GitHub Actions uses `ubuntu-latest` + `actions/setup-*`).
  Verification: the baseline is enforced in CI via PR checks (`.github/workflows/ci.yml`).

## 3. Baseline Checks (Phase 0)
All commands are runnable from the repo root. Artifacts are produced under `artifacts/ci/<check_id>/`.

Note (CI governance):
- PR descriptions are validated fail-closed (mandatory sections/checklists) via `tools/ci/check-pr-governance.sh`.

Executed checks (local) + evidence paths:
```bash
bash tools/ci/bin/run.sh preflight
bash tools/ci/bin/run.sh docs-links-full
bash tools/ci/bin/run.sh build
bash tools/ci/bin/run.sh api-contract
bash tools/ci/bin/run.sh pack
bash tools/ci/bin/run.sh consumer-smoke
bash tools/ci/bin/run.sh package-backed-tests
bash tools/ci/bin/run.sh security-nuget
bash tools/ci/bin/run.sh tests-bdd-coverage
bash tools/ci/bin/run.sh naming-snt
bash tools/ci/bin/run.sh versioning-svt
bash tools/ci/bin/run.sh version-convergence
```

Result (baseline, local): PASS for all checks listed above.
Evidence:
- `artifacts/ci/<check_id>/summary.md`
- `artifacts/ci/<check_id>/result.json`
- `artifacts/ci/<check_id>/raw.log`

Additional evidence (audit):
```bash
bash tools/audit/verify-code-analysis-evidence.sh
bash tools/audit/verify-security-claims.sh
```
Evidence:
- `artifacts/ci/code-analysis-evidence/summary.md`
- `artifacts/ci/security-claims-evidence/summary.md`

## 4. Inventory & Semantics Map (Phase 1)
Machine inventory (JSON, produced by `tools/audit/generate-code-analysis-json.sh`):
- `artifacts/audit/code_inventory.json`
- `artifacts/audit/callgraph_inventory.json`

Snapshot metrics (from inventory JSON):
- files (src, `.vb`/`.cs`): `22`
- LOC (src): `3918`
- method/sub declarations (heuristic): `683`

Note:
- Responsibility/intent per area is documented in module READMEs, e.g.:
  - `src/FileTypeDetection/README.md`
  - `src/FileTypeDetection/Infrastructure/README.md`
  - `src/FileTypeDetection/Detection/README.md`
  - `src/FileTypeDetection/Configuration/README.md`

## 5. Findings (Phase 2)
Sources (reproducible):
```bash
bash tools/audit/verify-code-analysis-evidence.sh
jq '.candidates | length' artifacts/audit/hardening_candidates.json
jq '.candidates | length' artifacts/audit/redundancy_candidates.json
jq '.candidates | length' artifacts/audit/dead_code_candidates.json
```

### Finding A (P1, hardening): Broad exception catches
Evidence:
- `artifacts/audit/hardening_candidates.json`

Status (baseline):
- candidates: `21`
- hotspots (count per file):
  - `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb` (8)
  - `src/FileTypeDetection/FileTypeDetector.vb` (5)
  - `src/FileTypeDetection/FileMaterializer.vb` (3)
  - `src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb` (2)
  - `src/FileTypeDetection/EvidenceHashing.vb` (1)
  - `src/FileTypeDetection/FileTypeOptions.vb` (1)
  - `src/FileTypeDetection/Infrastructure/CoreInternals.vb` (1)

Risk/impact:
- Catch-all can mask unexpected/programmatic errors; at the same time fail-closed behavior is required.

Fix strategy (package A):
- minimize/differentiate catch scopes (where deterministic).
- keep catch-all only as a non-fatal guard (do not swallow fatal exceptions) and with clear, stable reason logic.
- add regression tests for fail-closed branches/reason codes.

### Finding B (P2, maintainability): Guard/path handling duplication
Evidence:
- `artifacts/audit/redundancy_candidates.json`

Status (baseline):
- candidates: `18` (heuristic; not every entry is actionable).

Fix strategy (package B):
- consolidate duplicates only if outputs remain byte-for-byte identical.
- add edge-case regression tests for guards (traversal, root, invalid path, staging cleanup).

### Finding C (P3, report-only): Dead-code candidates
Evidence:
- `artifacts/audit/dead_code_candidates.json`

Status (baseline):
- candidates: `0` (heuristic-based).

## 6. PR Mapping (Issue #67 deliverables)
Planned/iterative PR packages (from `docs/audit/110_REFACTOR_BACKLOG.MD`):
1. Package A (P1) - Narrow broad exception catches in high-traffic entry points
2. Package B (P2) - Archive/path guard duplication reduction
3. Package C (P3) - Exception reason code normalization

This document is cross-PR; per package, the findings/status section is updated.

## 7. Decision Log
- 2026-02-15: baseline evidence is produced exclusively via reproducible commands + `artifacts/` (do not commit).
- 2026-02-15: initial finding source is a JSON heuristic (`tools/audit/generate-code-analysis-json.sh`), then hardened by targeted tests/evidence.
