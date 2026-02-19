# Index - Unit

## Zweck

Direkter Nachweis von API-Korrektheit, Security-Regeln und Determinismus.

## Testabdeckung

| Testdatei                                                | Fokus                                                                         |
|----------------------------------------------------------|-------------------------------------------------------------------------------|
| `FileTypeRegistryUnitTests.cs`                           | deterministisches Typ-/Alias-Mapping                                          |
| `HeaderCoveragePolicyUnitTests.cs`                       | Header-/Content-Coverage-Policy                                               |
| `HeaderOnlyPolicyUnitTests.cs`                           | Policy-Verhalten für Nicht-Archiv/Archiv                                      |
| `HeaderDetectionWarningUnitTests.cs`                     | Logging-/Warnpfade                                                            |
| `ExtensionCheckUnitTests.cs`                             | Endungsregeln und fail-closed                                                 |
| `FileTypeProjectBaselineUnitTests.cs`                    | Baseline-Defaults                                                             |
| `FileTypeDetectorFacadeUnitTests.cs`                     | Root-API für Typdetektion und Endungsprüfung                                  |
| `DetectionDetailAndArchiveValidationUnitTests.cs`        | `DetectDetailed` + `TryValidateArchive`                                       |
| `ArchiveProcessingFacadeUnitTests.cs`                    | statische Archiv-Fassade                                                      |
| `FileMaterializerUnitTests.cs`                           | Byte-basierte Persistenz + optional sichere Archiv-Extraktion                 |
| `FileTypeOptionsFacadeUnitTests.cs`                      | zentrale JSON-Optionsschnittstelle                                            |
| `ZipExtractedEntryUnitTests.cs`                          | In-Memory ZIP-Entry Objekt                                                    |
| `MimeProviderUnitTests.cs`                               | MIME-Mapping Infrastruktur                                                    |
| `DetectionDetailUnitTests.cs`                            | Detektionsdetail-Defaults                                                     |
| `HashingEvidenceTests.cs`                                | Hashing: Physical/Logical SHA-256, xxHash3, optional HMAC-SHA256, fail-closed Pfade, RoundTrip (h1-h4), API-Contract |
| `CoreInternalsAdditionalUnitTests.cs`                    | weitere Core-Guards/Policies                                                  |
| `ArchiveDescriptorUnitTests.cs`                          | Descriptor/Container-Logik                                                    |
| `ArchiveManagedBackendUnitTests.cs`                      | Managed ZIP Backend + Entry-Model                                             |
| `ArchiveInternalsUnitTests.cs`                           | Registry/Descriptor/EntryModel-Defaults in Archive-Internals                  |
| `ArchiveAdversarialTests.cs`                             | adversarial Archiv-Fälle                                                      |
| `ArchiveExtractionUnitTests.cs`                          | sichere Disk-/Memory-Extraktion                                               |
| `CoreAndArchiveInternalsFailClosedUnitTests.cs`          | gezielte Fail-closed/Guard-Branches in `CoreInternals` und `ArchiveInternals` |
| `OpenXmlRefinerUnitTests.cs`                             | OpenXML-Refinement (Docx/Xlsx/Pptx)                                           |
| `ArchiveInternalsReflectionUnitTests.cs`                 | reflektierte Tests fuer Archiv-Guards und Entry-Checks                        |
| `ArchiveExtractorAdditionalUnitTests.cs`                 | Extra-Branches fuer Extractor (Invalids/Nulls)                                |
| `ArchiveExtractorEndToEndUnitTests.cs`                   | End-to-End Extract für ZIP-Streams                                            |
| `LogGuardUnitTests.cs`                                   | Logger-Guard faengt Exceptions ab                                             |
| `ArchiveStreamEngineUnitTests.cs`                        | Managed ZIP Stream-Engine Limits (EntryCount/Compression/Nesting)             |
| `ArchiveStreamEngineExtraUnitTests.cs`                   | Extra Branches fuer ArchiveStreamEngine                                       |
| `FileTypeDetectorEdgeUnitTests.cs`                       | Edge-Cases fuer DetectDetailed/ReasonCodes                                    |
| `FileTypeDetectorReflectionUnitTests.cs`                 | Reflection-Branches fuer ReadHeader                                           |
| `ArchiveInternalsPrivateBranchUnitTests.cs`              | Private Branches in ArchiveInternals (Size/Recursive)                         |
| `CoreInternalsBranchUnitTests.cs`                        | Core-Guard Branches (Payload/Path/Policy)                                     |
| `ArchiveExtractorReflectionUnitTests.cs`                 | Reflection-Branches fuer ExtractEntry-ToDirectory/Memory                      |
| `ArchiveInternalsEarlyReturnUnitTests.cs`                | Early-return Branches in ArchiveInternals                                     |
| `ArchiveManagedInternalsExtraUnitTests.cs`               | Extra Branches fuer ArchiveStreamEngine                                       |
| `CoreInternalsStreamUnitTests.cs`                        | Unreadable-Stream Branch in ArchiveSafetyGate                                 |
| `ArchiveInternalsNestedBranchUnitTests.cs`               | Nested GZip Branches (SharpCompress)                                          |
| `FileTypeDetectorExtensionUnitTests.cs`                  | Extension-Policy Branches                                                     |
| `ArchiveEntryCollectorUnitTests.cs`                      | Collector Fail/Success Branches                                               |
| `ArchiveTypeResolverAdditionalUnitTests.cs`              | Stream/Bytes Branches in ArchiveTypeResolver                                  |
| `ArchiveTypeResolverExceptionUnitTests.cs`               | Exception-Pfade in ArchiveTypeResolver                                        |
| `SharpCompressArchiveBackendUnitTests.cs`                | Branches fuer SharpCompress-Backend                                           |
| `ArchiveSharpCompressCompatUnitTests.cs`                 | Contract-Guards fuer SharpCompress-Kompat-Schicht                             |
| `SharpCompressEntryModelUnitTests.cs`                    | Null-Entry Defaults im SharpCompressEntryModel                                |
| `SharpCompressEntryModelNonNullUnitTests.cs`             | Real-Entry Pfade im SharpCompressEntryModel                                   |
| `FileTypeDetectorAdditionalUnitTests.cs`                 | LoadOptions/ReadFileSafe/Detect Branches                                      |
| `FileTypeDetectorPrivateBranchUnitTests.cs`              | Private Branches via Reflection                                               |

Hinweis: Keine Coverage-Excludes (maximal strikt).

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
