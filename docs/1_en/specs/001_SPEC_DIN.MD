<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/specs/001_SPEC_DIN.MD) | [EN](001_SPEC_DIN.MD)
<!-- LANG_SWITCH:END -->

# DIN-oriented Specification (EN) - FileTypeDetection

## 1. Document control
- Document ID: `FTD-DIN-SPEC-EN`
- Version: `1.1`
- Language: English
- Scope: `src/FileTypeDetection`

## 2. Purpose and application scope
This specification describes requirements and evidence for the module's public API in a norm-oriented structure (requirements, architecture, verification, traceability).
This document serves as normative evidence for release, audit, and compliance processes.
The technical details of the public interfaces are documented in `docs/110_API_CORE.MD`, `docs/120_ARCH_CORE.MD`, and `docs/references/101_REFERENCES_CORE.MD`.

## 3. Terms and definitions
- `fail-closed`: error path returns only safe default values.
- `SSOT`: single source of truth for detection rules.
- `archive gate`: security-oriented validation before archive processing.

## 4. Normative requirements
| ID | Requirement | Must criterion | Evidence |
|---|---|---|---|
| FTD-REQ-001 | Deterministic type detection | same input => same `FileKind` | `FileTypeRegistryUnitTests.cs` |
| FTD-REQ-002 | Fail-closed on errors | no exception outward; instead `Unknown`/`False`/empty | `ArchiveAdversarialTests.cs`, `DetectionDetailAndArchiveValidationUnitTests.cs` |
| FTD-REQ-003 | Archive safety before extraction | traversal, bombs, and exceeded limits are rejected | `ArchiveExtractionUnitTests.cs`, `ArchiveGatePropertyTests.cs` |
| FTD-REQ-004 | Central options management | load/read global options only via `FileTypeOptions` | `FileTypeOptionsFacadeUnitTests.cs` |
| FTD-REQ-005 | Unified byte persistence | byte persistence with `overwrite`/`secureExtract` in `FileMaterializer` | `FileMaterializerUnitTests.cs` |
| FTD-REQ-006 | Archive alias normalization | `tar/tgz/gz/bz2/xz/7z/rar/zz` are treated as logical archive kind `FileKind.Zip` via aliases | `FileTypeRegistryUnitTests.cs`, `ExtensionCheckUnitTests.cs` |
| FTD-REQ-007 | Defensive destination path safety | root paths must not be materialized; size limit must apply | `FileMaterializerUnitTests.cs` |
| FTD-REQ-008 | Defensive archive extraction targets | root paths must not be used as archive extraction targets | `ArchiveExtractionUnitTests.cs` |

## 5. Architecture and responsibilities
| Class | Responsibility | Non-goal |
|---|---|---|
| `FileTypeDetector` | detect file types and run safety checks | no persistence |
| `ArchiveProcessing` | safe archive validation and in-memory extraction | no general byte persistence and no disk write |
| `FileMaterializer` | byte persistence to file/directory with optional archive extract | no new archive policy |
| `FileTypeOptions` | central global options surface (`LoadOptions`, `GetOptions`) | no type detection |

## 6. Interfaces and parameters
- `FileMaterializer.Persist(data, destinationPath, overwrite, secureExtract)`
  - `overwrite` default: `False`
  - `secureExtract` default: `False`
- `FileTypeOptions.LoadOptions(json)`
  - partial JSON values override defaults
- `FileTypeOptions.GetOptions()`
  - returns a JSON snapshot

## 7. Verification
Mandatory run for release:
```bash
python3 tools/check-docs.py
dotnet restore FileClassifier.sln -v minimal
dotnet build FileClassifier.sln --no-restore -v minimal
TEST_BDD_OUTPUT_DIR=artifacts/tests bash tools/test-bdd-readable.sh -- \
  /p:CollectCoverage=true \
  /p:Include="[FileTypeDetectionLib]*" \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput="$(pwd)/artifacts/coverage/coverage" \
  /p:Threshold=85%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
```

## 8. Traceability
This specification is traceable to:
- function reference: `docs/110_API_CORE.MD`
- architecture and flow reference: `docs/120_ARCH_CORE.MD`
- reference document: `docs/references/101_REFERENCES_CORE.MD`
- module index: `src/FileTypeDetection/README.md`
- unit/property tests: `tests/FileTypeDetectionLib.Tests`

## Documentation Maintenance Checklist
- [ ] Content verified against current code state.
- [ ] Links and anchors checked with `python3 tools/check-docs.py`.
- [ ] Examples/commands verified locally.
- [ ] Terminology aligned with `docs/110_API_CORE.MD`.
