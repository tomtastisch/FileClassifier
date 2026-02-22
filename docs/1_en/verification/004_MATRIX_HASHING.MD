<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/verification/004_MATRIX_HASHING.MD) | [EN](004_MATRIX_HASHING.MD)
<!-- LANG_SWITCH:END -->

# Test Matrix - Deterministic Hashing

## 1. Purpose & Scope
This matrix maps the full hashing pipeline (file/bytes/archive) to concrete unit, integration, and E2E tests.
Goal: traceable evidence per pipeline stage for positive and negative cases.

## 2. Definitions
- `PhysicalHash`: SHA-256 over raw bytes.
- `LogicalHash`: SHA-256 over canonicalized content state.
- `h1..h4`: roundtrip evidence (`archive-input -> extracted -> bytes -> materialized`).
- `fail-closed`: unsafe inputs return safe outputs.

## 3. Pipeline Stage Matrix
| Stage | Positive cases | Negative cases | Concrete tests (evidence) |
|---|---|---|---|
| S1 Input + classification | file/bytes are correctly recognized as archive or non-archive | missing file / non-archive stays fail-closed | `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs` (`HashFile_MissingPath_FailsClosedWithoutThrowing`, `VerifyRoundTrip_ProducesLogicalConsistency`), `tests/FileTypeDetectionLib.Tests/Unit/CoreAndArchiveInternalsFailClosedUnitTests.cs` (`ArchiveTypeResolver_TryDescribeBytes_MapsContainerTypeDeterministically`, `ArchiveTypeResolver_TryDescribeBytes_FailsClosed_ForNonArchivePayload`) |
| S2 Archive validation | archive payload is validatable before extraction | broken or unsafe payload is rejected | `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs` (`ArchivePipeline_PreservesCombinedAndPerFileHashes_AfterExtractByteMaterializeAndRecheck`, `ArchiveBytePipeline_PreservesCombinedAndPerFileHashes_AfterExtractAndMaterialize`, `UnsafeArchiveCandidate_FailsExtraction_AndFallsBackToArchiveByteHashing`), `tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_050_HASHING_UND_ROUNDTRIP.feature` (`Nicht-Archiv- oder defekte Bytes...`), `tests/FileTypeDetectionLib.Tests/Unit/CoreAndArchiveInternalsFailClosedUnitTests.cs` (`ArchiveSafetyGate_FailsClosed_ForInvalidInputs`) |
| S3 Extraction -> entry set | entries are deterministically transferred into memory | traversal/unsafe entries are blocked fail-closed | `tests/FileTypeDetectionLib.Tests/Unit/ArchiveProcessingFacadeUnitTests.cs` (`TryExtractToMemory_ReturnsEntries_ForValidArchiveBytes`), `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs` (`HashBytes_FallsBackToArchiveByteMode_WhenArchivePayloadIsUnsafe`) |
| S4 Hashing per entry | per-entry `HashBytes` is stable and reproducible | path traversal in `HashEntries` is rejected | `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs` (`HashBytes_ReturnsStableDigests_ForSamePayload`, `HashEntries_FailsClosed_ForPathTraversal`) |
| S5 Materialization + rehash | entry bytes and materialized files yield identical hashes | invalid destination paths/overwrite conflicts are fail-closed | `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs` (`ArchivePipeline_PreservesCombinedAndPerFileHashes_AfterExtractByteMaterializeAndRecheck`, `ArchiveBytePipeline_PreservesCombinedAndPerFileHashes_AfterExtractAndMaterialize`), `tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_050_HASHING_UND_ROUNDTRIP.feature` (`Archiv-Entry bleibt beim Byte->Materializer->Byte Zyklus hash-stabil`) |
| S6 Combined hash + roundtrip h1..h4 | combined logical hash remains identical across all stages | non-archived file bytes work deterministically without extraction | `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs` (`VerifyRoundTrip_ProducesLogicalConsistency`, `HashFile_And_HashBytes_Match_ForPlainFile`) |
| S7 API contract stability | public EvidenceHashing signatures are frozen | contract deviations fail tests | `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs` (`PublicStaticSurface_MatchesApprovedContract`) |

## 4. Format Matrix (Hashing-relevante Abdeckung)
| Format | Direct hashing | Validate/extract | Materialize/rehash | Evidence |
|---|---|---|---|---|
| ZIP | Yes | Yes | Yes | `HashingEvidenceRoundTripTests` + `FTD_BDD_050...` |
| RAR | Yes | Yes | Yes | `HashingEvidenceRoundTripTests` + `FTD_BDD_050...` |
| 7z | Yes | Yes | Yes | `HashingEvidenceRoundTripTests` + `FTD_BDD_050...` |
| TAR | Yes (logical via bytes) | Yes (generated payload) | Yes | `HashingEvidenceRoundTripTests` (`LogicalHash_IsStableAcrossArchiveTarAndTarGz_ForSameContent`) |
| TAR.GZ | Yes (logical via bytes) | Yes (generated payload) | Yes | `HashingEvidenceRoundTripTests` (`LogicalHash_IsStableAcrossArchiveTarAndTarGz_ForSameContent`) |
| Raw file (e.g. PDF) | Yes | N/A | Yes | `HashingEvidenceSha256Tests` (`HashFile_And_HashBytes_Match_ForPlainFile`), `FTD_BDD_050...` (`Direkte Datei-Bytes...`) |

## 5. Execution Commands
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal --filter "FullyQualifiedName~HashingEvidence"
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal --filter "Category=hashing|Category=roundtrip|Category=archive"
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal /p:CollectCoverage=true /p:Include=\"[FileTypeDetectionLib]*\" /p:CoverletOutputFormat=cobertura /p:CoverletOutput=TestResults/coverage/
```

## 6. Open Coverage Gap (for 90-95%)
## 6. Open Coverage Gap (for 90-95%)
- Currently measured: line `86.03%`, branch `70.06%` (full suite).
- Missing steps to 90-95%: targeted branch tests for fail-closed guards in `src/FileTypeDetection/Infrastructure/CoreInternals.vb` and `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb`.

## Documentation Maintenance Checklist
- [ ] Content verified against current code state.
- [ ] Links and anchors checked with `python3 tools/check-docs.py`.
- [ ] Examples/commands verified locally.
- [ ] Terminology aligned with `docs/110_API_CORE.MD`.
