# Index - Unit

## Zweck
Direkter Nachweis von API-Korrektheit, Security-Regeln und Determinismus.

## Testabdeckung
| Testdatei | Fokus |
|---|---|
| `FileTypeRegistryUnitTests.cs` | deterministisches Typ-/Alias-Mapping |
| `HeaderCoveragePolicyUnitTests.cs` | Header-/Content-Coverage-Policy |
| `HeaderOnlyPolicyUnitTests.cs` | Policy-Verhalten für Nicht-Archiv/Archiv |
| `HeaderDetectionWarningUnitTests.cs` | Logging-/Warnpfade |
| `ExtensionCheckUnitTests.cs` | Endungsregeln und fail-closed |
| `FileTypeProjectBaselineUnitTests.cs` | Baseline-Defaults |
| `FileTypeDetectorFacadeUnitTests.cs` | Root-API für Typdetektion und Endungsprüfung |
| `DetectionDetailAndArchiveValidationUnitTests.cs` | `DetectDetailed` + `TryValidateArchive` |
| `ArchiveProcessingFacadeUnitTests.cs` | statische Archiv-Fassade |
| `FileMaterializerUnitTests.cs` | Byte-basierte Persistenz + optional sichere Archiv-Extraktion |
| `FileTypeOptionsFacadeUnitTests.cs` | zentrale JSON-Optionsschnittstelle |
| `ZipExtractedEntryUnitTests.cs` | In-Memory ZIP-Entry Objekt |
| `MimeProviderUnitTests.cs` | MIME-Mapping Infrastruktur |
| `DetectionDetailUnitTests.cs` | Detektionsdetail-Defaults |
| `DeterministicHashDigestSetUnitTests.cs` | Hash-Sets Normalisierung |
| `CoreInternalsAdditionalUnitTests.cs` | weitere Core-Guards/Policies |
| `ArchiveDescriptorUnitTests.cs` | Descriptor/Container-Logik |
| `ArchiveManagedBackendUnitTests.cs` | Managed ZIP Backend + Entry-Model |
| `ArchiveInternalsUnitTests.cs` | Registry/Descriptor/EntryModel-Defaults in Archive-Internals |
| `ArchiveAdversarialTests.cs` | adversarial Archiv-Fälle |
| `ArchiveExtractionUnitTests.cs` | sichere Disk-/Memory-Extraktion |
| `CoreAndArchiveInternalsFailClosedUnitTests.cs` | gezielte Fail-closed/Guard-Branches in `CoreInternals` und `ArchiveInternals` |
| `DeterministicHashingUnitTests.cs` | deterministische Physical/Logical Hash-Evidence inkl. fail-closed Pfade |
| `DeterministicHashingFailureUnitTests.cs` | deterministische Hash-Failures und RoundTrip-Fehlerpfade |
| `DeterministicHashingApiContractUnitTests.cs` | eingefrorener Public API-Contract von `DeterministicHashing` |
| `OpenXmlRefinerUnitTests.cs` | OpenXML-Refinement (Docx/Xlsx/Pptx) |
| `ArchiveInternalsReflectionUnitTests.cs` | reflektierte Tests fuer Archiv-Guards und Entry-Checks |
| `DeterministicHashingEdgeUnitTests.cs` | Edge-Cases fuer Hash-Labels/Empty-Entries |
| `LogGuardUnitTests.cs` | Logger-Guard faengt Exceptions ab |
| `ArchiveStreamEngineUnitTests.cs` | Managed ZIP Stream-Engine Limits (EntryCount/Compression/Nesting) |
| `DeterministicHashEvidenceUnitTests.cs` | Hash-Evidence Defaults/Null-Handling |
| `DeterministicHashRoundTripReportUnitTests.cs` | RoundTrip-Report Konsistenzlogik |
| `DeterministicHashOptionsUnitTests.cs` | DeterministicHashOptions Normalisierung und Defaults |
| `DeterministicHashRoundTripReportReflectionUnitTests.cs` | Reflection-Branches fuer RoundTrip-Report |

Hinweis: Coverage-Excludes: `*Internals.vb` (Datei-Pattern) sowie `DeterministicHashing` und `FileTypeDetector` (ExcludeFromCodeCoverage).

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
