<!-- LANG_SWITCH:BEGIN -->
[DE](010_REFACTOR_BACKLOG.MD) | [EN](../../1_en/audit/010_REFACTOR_BACKLOG.MD)
<!-- LANG_SWITCH:END -->

# Refactor-Backlog (Cluster 7C)

## 1. Quelle
Der Backlog wird abgeleitet aus:
- `artifacts/audit/hardening_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `docs/audit/006_CODE_REVIEW_FINDINGS.MD`

Baseline Snapshot (2026-02-15, vor Package A):
- hardening candidates: `21`
- redundancy candidates: `18`
- dead-code candidates: `0`

Current Snapshot (2026-02-15, nach Package A):
- hardening candidates: `12`

Repro (repo root):
```bash
bash tools/audit/verify-code-analysis-evidence.sh
jq '.candidates | length' artifacts/audit/hardening_candidates.json
```

## 2. Priorisierte PR-Packages

### Package A (P1) - Broad exception catches in high-traffic entry points einengen
Scope candidates:
- `src/FileTypeDetection/FileTypeDetector.vb`
- `src/FileTypeDetection/FileMaterializer.vb`
- `src/FileTypeDetection/EvidenceHashing.vb`

Rules:
- generisches `Catch ex As Exception` durch engere Exception-Behandlung ersetzen, wo deterministisch möglich
- fail-closed Verhalten beibehalten
- aktuelle public API und Return-Contracts erhalten

Required checks:
Alle Kommandos sind für Ausführung im Repository-Root gedacht.
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
- duplizierte Guard-Fragmente nur konsolidieren, wenn das Verhalten auf Outputs byte-for-byte äquivalent bleibt
- fokussierte Regression-Tests für Guard-Edge-Cases ergänzen

Required checks:
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release --filter "Category=ApiContract" -v minimal
bash tools/ci/bin/run.sh package-backed-tests
bash tools/ci/bin/run.sh consumer-smoke
```

### Package C (P3) - Exception reason code normalization
Scope candidates:
- broad catch sites, die nach Package A bewusst generisch bleiben

Rules:
- Exception-to-Result-Mapping mit deterministischen Reason-Codes anreichern
- keine versteckten Behavior-Changes im Success-Pfad

Required checks:
```bash
dotnet build FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -c Release -v minimal
bash tools/audit/verify-code-analysis-evidence.sh
```

## 3. Ausführungsreihenfolge
1. Package A
2. Package B
3. Package C

## 4. Definition of Done pro Package
- Targeted Code-Changes sind auf den Package-Scope begrenzt
- Regression-Tests sind für geänderte Behavior-Branches hinzugefügt/aktualisiert
- Full CI auf der PR ist grün
- `docs/audit/006_CODE_REVIEW_FINDINGS.MD` wurde mit geschlossenen/verschobenen Findings aktualisiert
