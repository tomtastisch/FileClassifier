# Refactor Backlog (Cluster 7C)

## 1. Source
Backlog is derived from:
- `artifacts/audit/hardening_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `docs/audit/006_CODE_REVIEW_FINDINGS.MD`

Current baseline snapshot:
- hardening candidates: `12`
- redundancy candidates: `18`
- dead-code candidates: `0`

## 2. Prioritized PR Packages

### Package A (P1) - Narrow broad exception catches in high-traffic entry points
Scope candidates:
- `src/FileTypeDetection/FileTypeDetector.vb`
- `src/FileTypeDetection/FileMaterializer.vb`
- `src/FileTypeDetection/EvidenceHashing.vb`

Rules:
- replace generic `Catch ex As Exception` with narrower exception handling where deterministically possible
- keep fail-closed behavior intact
- preserve current public API and return contracts

Required checks:
```bash
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/ci/bin/run.sh tests-bdd-coverage
```

### Package B (P2) - Archive/path guard duplication reduction
Scope candidates:
- `src/FileTypeDetection/Infrastructure/CoreInternals.vb`
- `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb`
- `src/FileTypeDetection/Infrastructure/ArchiveManagedInternals.vb`

Rules:
- consolidate duplicated guard fragments only when behavior remains byte-for-byte equivalent on outputs
- add focused regression tests for guard edge cases

Required checks:
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release --filter "Category=ApiContract" -v minimal
bash tools/ci/bin/run.sh package-backed-tests
bash tools/ci/bin/run.sh consumer-smoke
```

### Package C (P3) - Exception reason code normalization
Scope candidates:
- broad catch sites that remain intentionally generic after Package A

Rules:
- enrich exception-to-result mapping with deterministic reason codes
- no hidden behavior changes in success path

Required checks:
```bash
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/audit/verify-code-analysis-evidence.sh
```

## 3. Execution Order
1. Package A
2. Package B
3. Package C

## 4. Definition Of Done Per Package
- targeted code changes are limited to package scope
- regression tests added/updated for changed behavior branches
- full CI on PR is green
- `docs/audit/006_CODE_REVIEW_FINDINGS.MD` updated with closed/moved findings
