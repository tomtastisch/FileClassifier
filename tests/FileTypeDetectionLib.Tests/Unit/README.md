# Index - Unit

## Zweck
Direkter Nachweis von API-Korrektheit, Security-Regeln und Determinismus.

## Testabdeckung
| Testdatei | Fokus |
|---|---|
| `FileTypeRegistryUnitTests.cs` | deterministisches Typ-/Alias-Mapping |
| `HeaderCoveragePolicyUnitTests.cs` | Header-/Content-Coverage-Policy |
| `HeaderOnlyPolicyUnitTests.cs` | Policy-Verhalten fuer Nicht-Archiv/Archiv |
| `HeaderDetectionWarningUnitTests.cs` | Logging-/Warnpfade |
| `ExtensionCheckUnitTests.cs` | Endungsregeln und fail-closed |
| `FileTypeProjectBaselineUnitTests.cs` | Baseline-Defaults |
| `FileTypeDetectorFacadeUnitTests.cs` | Root-API fuer Typdetektion und Endungspruefung |
| `DetectionDetailAndArchiveValidationUnitTests.cs` | `DetectDetailed` + `TryValidateArchive` |
| `ArchiveProcessingFacadeUnitTests.cs` | statische Archiv-Fassade |
| `FileMaterializerUnitTests.cs` | Byte-basierte Persistenz + optional sichere Archiv-Extraktion |
| `FileTypeOptionsFacadeUnitTests.cs` | zentrale JSON-Optionsschnittstelle |
| `ArchiveAdversarialTests.cs` | adversarial Archiv-Faelle |
| `ArchiveExtractionUnitTests.cs` | sichere Disk-/Memory-Extraktion |
| `CoreAndArchiveInternalsFailClosedUnitTests.cs` | gezielte Fail-closed/Guard-Branches in `CoreInternals` und `ArchiveInternals` |
| `DeterministicHashingUnitTests.cs` | deterministische Physical/Logical Hash-Evidence inkl. fail-closed Pfade |
| `DeterministicHashingApiContractUnitTests.cs` | eingefrorener Public API-Contract von `DeterministicHashing` |
