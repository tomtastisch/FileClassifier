# Index - tests/FileTypeDetectionLib.Tests

## 1. Zweck

Nachweis für Sicherheit, Determinismus, Korrektheit und API-Klarheit.

## 2. Testkategorien

| Kategorie                           | Fokus                                                                | Referenz                                         |
|-------------------------------------|----------------------------------------------------------------------|--------------------------------------------------|
| Unit                                | API- und Regelverhalten                                              | [Index - Unit](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/README.md)               |
| Integration                         | format- und pipelineübergreifende Nachweise mit echten Archiv-Pfaden | [Index - Integration](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Integration/README.md) |
| Property                            | Grenz-/Invarianztests für Archiv-Gate, Optionen und Materializer     | [Index - Property](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Property/README.md)       |
| Features (BDD Unit/Integration/E2E) | fachliche Lesbarkeit und End-to-End-Flows                            | [Index - Features](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Features/README.md)       |
| Benchmarks                          | regressionsorientierte Laufzeittrends                                | [Index - Benchmarks](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Benchmarks/README.md)   |

## 3. Nachweismatrix

| Qualitätsziel                                                                                                                | Testdatei                                                                                                                                                                                                                                                                                                                                      |
|------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Fail-closed Archiv-Grenzen                                                                                                   | [Archiveadversarialtests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/ArchiveAdversarialTests.cs)                                                                                                                                                                                                                                                                           |
| Sichere Archiv-Extraktion                                                                                                    | [Archiveextractionunittests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/ArchiveExtractionUnitTests.cs)                                                                                                                                                                                                                                                                     |
| Unified-Archive-Flows (mehrere Archivformate für Byte-Array Detect/Validate/Extract/Materialize)                             | [Ftd Bdd 040 Archive Typen Bytearray Und Materialisierung](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_040_ARCHIVE_TYPEN_BYTEARRAY_UND_MATERIALISIERUNG.feature), [Unifiedarchivebackendunittests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/UnifiedArchiveBackendUnitTests.cs)                                                                                                   |
| Deterministische Hash-Evidence + h1-h4 RoundTrip + Extract->Bytes->Materialize Invarianz (Physical/Logical, positiv/negativ) | [Hashingevidencetests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs), [Ftd Bdd 050 Hashing Und Roundtrip](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_050_HASHING_UND_ROUNDTRIP.feature) |
| API-Contract-Freeze für `EvidenceHashing`                                                                               | [Hashingevidencetests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs), [04 - EvidenceHashing API Contract](https://github.com/tomtastisch/FileClassifier/blob/main/docs/contracts/001_CONTRACT_HASHING.MD)                                                                                                                             |
| Hashing-Pipeline Traceability (Stage-by-Stage Matrix)                                                                        | [Test Matrix - Deterministic Hashing](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/004_MATRIX_HASHING.MD)                                                                                                                                                                                                                                                                         |
| Deterministische Registry                                                                                                    | [Filetyperegistryunittests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/FileTypeRegistryUnitTests.cs)                                                                                                                                                                                                                                                                       |
| API Detail-/Archiv-Fassade                                                                                                   | [Detectiondetailandarchivevalidationunittests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/DetectionDetailAndArchiveValidationUnitTests.cs), [Archiveprocessingfacadeunittests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/ArchiveProcessingFacadeUnitTests.cs)                                                                                                                                         |
| Options-/Materializer-Invarianten                                                                                            | [Filetypeoptionspropertytests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Property/FileTypeOptionsPropertyTests.cs), [Filematerializerpropertytests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Property/FileMaterializerPropertyTests.cs)                                                                                                                                                               |

## 3.1 Formatbezogene Testabdeckung (Stand)

| Format | Detection                       | Validate                        | Extract Memory                  | Extract Disk                    |
|--------|---------------------------------|---------------------------------|---------------------------------|---------------------------------|
| ZIP    | abgedeckt                       | abgedeckt                       | abgedeckt                       | abgedeckt                       |
| TAR    | abgedeckt                       | indirekt über Unified-Pipeline  | implizit über Unified-Pipeline  | implizit über Unified-Pipeline  |
| TAR.GZ | abgedeckt                       | abgedeckt                       | abgedeckt                       | abgedeckt                       |
| 7z     | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) |
| RAR    | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) |

## 4. Ausführung

Vollständige Ausführung (ohne Filter, führt alle Tests aus):

```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal
```

Alternative auf Solution-Ebene:

```bash
dotnet test FileClassifier.sln --no-build -v minimal
```

## 4.1 Fixture-Governance (alle Ressourcen)

- SSOT: `tests/FileTypeDetectionLib.Tests/resources/fixtures.manifest.json`
- Jede Ressource besitzt:
    - `fixtureId` (kanonischer, stabiler Identifier; bevorzugt für Referenzen),
    - `dataType` (fachliche Klassifikation),
    - `objectId` (`sha256:<hex>` als content-addressed Objekt-ID),
    - `sha256` (Integritätsnachweis),
    - `sourceUrl`/`sourceRef` (Herkunft).
- Beim ersten Zugriff validiert `TestResources` den kompletten Ressourcenbestand fail-closed:
    - Hash-Mismatch, fehlende Manifest-Einträge oder unregistrierte Dateien => Testfehler.

## 5. Tag-Filter (Reqnroll-Annotationen)

Filter werden über `--filter "Category=<tag>"` (ohne `@`) als Suffix angehängt.
Ohne Filter-Suffix werden immer alle Tests ausgeführt.
Basisbefehl für alle Tabellen unten:

```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal
```

### 5.1 Annotationen nach Testtyp

| Annotation     | Bedeutung                                   | Filter-Suffix (nur diese Tests)   |
|----------------|---------------------------------------------|-----------------------------------|
| `@unit`        | BDD-Tests mit unit-nahem Fokus              | `--filter "Category=unit"`        |
| `@integration` | BDD-Integrationsflüsse zwischen Komponenten | `--filter "Category=integration"` |
| `@e2e`         | End-to-End-Flüsse inkl. Dateisystempfaden   | `--filter "Category=e2e"`         |
| `@positiv`     | erwartete Erfolgsfälle                      | `--filter "Category=positiv"`     |
| `@negativ`     | fail-closed-, Fehler- und Grenzfälle        | `--filter "Category=negativ"`     |

### 5.2 Annotationen nach Klasse/Komponente

| Annotation      | Bedeutung                                        | Filter-Suffix (nur diese Tests)    |
|-----------------|--------------------------------------------------|------------------------------------|
| `@detector`     | Fälle um `FileTypeDetector`                      | `--filter "Category=detector"`     |
| `@materializer` | Fälle um `FileMaterializer`                      | `--filter "Category=materializer"` |
| `@processing`   | Fälle um Pipeline/Fassaden (`ArchiveProcessing`) | `--filter "Category=processing"`   |
| `@archive`      | Archiv-bezogene Pfade (Validate/Refine/Extract)  | `--filter "Category=archive"`      |
| `@api`          | API-orientierte BDD-Fälle                        | `--filter "Category=api"`          |

### 5.3 Annotationen nach Fachthema

| Annotation       | Bedeutung                        | Filter-Suffix (nur diese Tests)     |
|------------------|----------------------------------|-------------------------------------|
| `@detektion`     | Signatur-/Typdetektion           | `--filter "Category=detektion"`     |
| `@endung`        | Endungs-Policy-Fälle             | `--filter "Category=endung"`        |
| `@lesen`         | sichere Datei-zu-Bytes-Lesepfade | `--filter "Category=lesen"`         |
| `@typpruefung`   | `IsOfType`-Prüfpfade             | `--filter "Category=typpruefung"`   |
| `@grenzen`       | MaxBytes-/Grenzwertpfade         | `--filter "Category=grenzen"`       |
| `@konfiguration` | Build-/Konfigurationspfade       | `--filter "Category=konfiguration"` |
| `@refinement`    | OOXML/Archiv-Refinement-Fälle    | `--filter "Category=refinement"`    |

Hinweis:

- Die Coverage-Konfiguration wird über den jeweils verwendeten Testlauf (`dotnet test` bzw. CI-Job) gesteuert; in diesem Repository sind dafür aktuell keine festen `ExcludeByFile`/`ExcludeByAttribute`-Filter in der Testprojektdatei hinterlegt.

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
