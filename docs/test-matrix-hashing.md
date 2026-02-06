# Test Matrix - Deterministic Hashing

## 1. Zweck & Scope
Diese Matrix mappt die komplette Hashing-Pipeline (Datei/Bytes/Archiv) auf konkrete Unit-, Integration- und E2E-Tests.
Ziel: tracebare Evidenz pro Pipeline-Stufe für positive und negative Fälle.

## 2. Definitions
- `PhysicalHash`: SHA-256 über Rohbytes.
- `LogicalHash`: SHA-256 über kanonisierten Inhaltszustand.
- `h1..h4`: RoundTrip-Nachweis (`archive-input -> extracted -> bytes -> materialized`).
- `fail-closed`: unsichere Eingaben liefern sichere Rückgaben.

## 3. Pipeline Stage Matrix
| Stage | Positiv-Fälle | Negativ-Fälle | Konkrete Tests (Evidenz) |
|---|---|---|---|
| S1 Eingang + Klassifikation | Datei/Bytes werden als Archiv oder Nicht-Archiv korrekt erkannt | nicht vorhandene Datei / nicht-Archiv bleibt fail-closed | `tests/FileTypeDetectionLib.Tests/Unit/DeterministicHashingApiContractUnitTests.cs` (`HashFile_MissingPath_FailsClosedWithoutThrowing`), `tests/FileTypeDetectionLib.Tests/Integration/DeterministicHashingIntegrationTests.cs` (`VerifyRoundTrip_ProducesLogicalConsistency`), `tests/FileTypeDetectionLib.Tests/Unit/CoreAndArchiveInternalsFailClosedUnitTests.cs` (`ArchiveTypeResolver_TryDescribeBytes_MapsContainerTypeDeterministically`, `ArchiveTypeResolver_TryDescribeBytes_FailsClosed_ForNonArchivePayload`) |
| S2 Archiv-Validierung | Archivpayload validierbar vor Extraktion | Defekte oder unsichere Payload wird abgewiesen | `tests/FileTypeDetectionLib.Tests/Integration/DeterministicHashingIntegrationTests.cs` (`ArchivePipeline_PreservesCombinedAndPerFileHashes_AfterExtractByteMaterializeAndRecheck`, `ArchiveBytePipeline_PreservesCombinedAndPerFileHashes_AfterExtractAndMaterialize`, `UnsafeArchiveCandidate_FailsExtraction_AndFallsBackToArchiveByteHashing`), `tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_050_DETERMINISTISCHES_HASHING_UND_ROUNDTRIP.feature` (`Nicht-Archiv- oder defekte Bytes...`), `tests/FileTypeDetectionLib.Tests/Unit/CoreAndArchiveInternalsFailClosedUnitTests.cs` (`ArchiveSafetyGate_FailsClosed_ForInvalidInputs`) |
| S3 Extraktion -> Entry-Set | Einträge werden deterministisch in Memory überführt | Traversal/unsichere Entries werden fail-closed blockiert | `tests/FileTypeDetectionLib.Tests/Unit/ArchiveProcessingFacadeUnitTests.cs` (`TryExtractToMemory_ReturnsEntries_ForValidArchiveBytes`), `tests/FileTypeDetectionLib.Tests/Unit/DeterministicHashingUnitTests.cs` (`HashBytes_FallsBackToArchiveByteMode_WhenArchivePayloadIsUnsafe`) |
| S4 Hashing pro Entry | `HashBytes` pro Entry ist stabil und reproduzierbar | Path-Traversal in `HashEntries` wird abgewiesen | `tests/FileTypeDetectionLib.Tests/Unit/DeterministicHashingUnitTests.cs` (`HashBytes_ReturnsStableDigests_ForSamePayload`, `HashEntries_FailsClosed_ForPathTraversal`) |
| S5 Materialisierung + Rehash | Entry-Bytes und materialisierte Datei liefern identische Hashes | ungültige Zielpfade/overwrite-Konflikte fail-closed | `tests/FileTypeDetectionLib.Tests/Integration/DeterministicHashingIntegrationTests.cs` (`ArchivePipeline_PreservesCombinedAndPerFileHashes_AfterExtractByteMaterializeAndRecheck`, `ArchiveBytePipeline_PreservesCombinedAndPerFileHashes_AfterExtractAndMaterialize`), `tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_050_DETERMINISTISCHES_HASHING_UND_ROUNDTRIP.feature` (`Archiv-Entry bleibt beim Byte->Materializer->Byte Zyklus hash-stabil`) |
| S6 Combined Hash + RoundTrip h1..h4 | Combined LogicalHash bleibt über alle Stufen identisch | nicht-archivierte Datei-Bytes arbeiten deterministisch ohne Extraktion | `tests/FileTypeDetectionLib.Tests/Integration/DeterministicHashingIntegrationTests.cs` (`VerifyRoundTrip_ProducesLogicalConsistency`), `tests/FileTypeDetectionLib.Tests/Unit/DeterministicHashingUnitTests.cs` (`HashFile_And_HashBytes_Match_ForPlainFile`) |
| S7 API-Contract Stabilität | Öffentliche DeterministicHashing-Signaturen sind eingefroren | Abweichungen vom Contract schlagen im Test fehl | `tests/FileTypeDetectionLib.Tests/Unit/DeterministicHashingApiContractUnitTests.cs` (`PublicStaticSurface_MatchesApprovedContract`) |

## 4. Format Matrix (Hashing-relevante Abdeckung)
| Format | Direktes Hashing | Validate/Extract | Materialize/Rehash | Nachweis |
|---|---|---|---|---|
| ZIP | Ja | Ja | Ja | `DeterministicHashingIntegrationTests` + `FTD_BDD_050...` |
| RAR | Ja | Ja | Ja | `DeterministicHashingIntegrationTests` + `FTD_BDD_050...` |
| 7z | Ja | Ja | Ja | `DeterministicHashingIntegrationTests` + `FTD_BDD_050...` |
| TAR | Ja (logisch via Bytes) | Ja (generated payload) | Ja | `DeterministicHashingIntegrationTests` (`LogicalHash_IsStableAcrossArchiveTarAndTarGz_ForSameContent`) |
| TAR.GZ | Ja (logisch via Bytes) | Ja (generated payload) | Ja | `DeterministicHashingIntegrationTests` (`LogicalHash_IsStableAcrossArchiveTarAndTarGz_ForSameContent`) |
| Raw File (z. B. PDF) | Ja | N/A | Ja | `DeterministicHashingUnitTests` (`HashFile_And_HashBytes_Match_ForPlainFile`), `FTD_BDD_050...` (`Direkte Datei-Bytes...`) |

## 5. Execution Commands
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal --filter "FullyQualifiedName~DeterministicHashing"
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal --filter "Category=hashing|Category=roundtrip|Category=archive"
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal /p:CollectCoverage=true /p:Include=\"[FileTypeDetectionLib]*\" /p:CoverletOutputFormat=cobertura /p:CoverletOutput=TestResults/coverage/
```

## 6. Open Coverage Gap (for 90-95%)
- Aktuell gemessen: Line `86.03%`, Branch `70.06%` (Gesamtsuite).
- Fehlende Schritte bis 90-95%: gezielte Branch-Tests für fail-closed Guards in `src/FileTypeDetection/Infrastructure/CoreInternals.vb` und `src/FileTypeDetection/Infrastructure/ArchiveInternals.vb`.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
