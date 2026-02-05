# Index - tests/FileTypeDetectionLib.Tests

## 1. Zweck
Nachweis fuer Sicherheit, Determinismus, Korrektheit und API-Klarheit.

## 2. Testkategorien
| Kategorie | Fokus | Referenz |
|---|---|---|
| Unit | API- und Regelverhalten | [Unit/README.md](./Unit/README.md) |
| Integration | format- und pipelineuebergreifende Nachweise mit echten Archiv-Pfaden | [Integration/README.md](./Integration/README.md) |
| Property | Grenz-/Invarianztests fuer ZIP-Gate, Optionen und Materializer | [Property/README.md](./Property/README.md) |
| Features (BDD Unit/Integration/E2E) | fachliche Lesbarkeit und End-to-End-Flows | [Features/README.md](./Features/README.md) |
| Benchmarks | regressionsorientierte Laufzeittrends | [Benchmarks/README.md](./Benchmarks/README.md) |

## 3. Nachweismatrix
| Qualitaetsziel | Testdatei |
|---|---|
| Fail-closed ZIP-Grenzen | [Unit/ZipAdversarialTests.cs](./Unit/ZipAdversarialTests.cs) |
| Sichere ZIP-Extraktion | [Unit/ZipExtractionUnitTests.cs](./Unit/ZipExtractionUnitTests.cs) |
| Unified-Archive-Flows (ZIP/TAR/TAR.GZ/7z/RAR fuer Byte-Array Detect/Validate/Extract/Materialize) | [Features/FTD_BDD_040_ARCHIVE_TYPEN_BYTEARRAY_UND_MATERIALISIERUNG.feature](./Features/FTD_BDD_040_ARCHIVE_TYPEN_BYTEARRAY_UND_MATERIALISIERUNG.feature), [Unit/UnifiedArchiveBackendUnitTests.cs](./Unit/UnifiedArchiveBackendUnitTests.cs) |
| Deterministische Hash-Evidence + h1-h4 RoundTrip (Physical/Logical) | [Unit/DeterministicHashingUnitTests.cs](./Unit/DeterministicHashingUnitTests.cs), [Integration/DeterministicHashingIntegrationTests.cs](./Integration/DeterministicHashingIntegrationTests.cs), [Features/FTD_BDD_050_DETERMINISTISCHES_HASHING_UND_ROUNDTRIP.feature](./Features/FTD_BDD_050_DETERMINISTISCHES_HASHING_UND_ROUNDTRIP.feature) |
| API-Contract-Freeze fuer `DeterministicHashing` | [Unit/DeterministicHashingApiContractUnitTests.cs](./Unit/DeterministicHashingApiContractUnitTests.cs), [../docs/04_DETERMINISTIC_HASHING_API_CONTRACT.md](../docs/04_DETERMINISTIC_HASHING_API_CONTRACT.md) |
| Deterministische Registry | [Unit/FileTypeRegistryUnitTests.cs](./Unit/FileTypeRegistryUnitTests.cs) |
| API Detail-/ZIP-Fassade | [Unit/DetectionDetailAndZipValidationUnitTests.cs](./Unit/DetectionDetailAndZipValidationUnitTests.cs), [Unit/ZipProcessingFacadeUnitTests.cs](./Unit/ZipProcessingFacadeUnitTests.cs) |
| Options-/Materializer-Invarianten | [Property/FileTypeOptionsPropertyTests.cs](./Property/FileTypeOptionsPropertyTests.cs), [Property/FileMaterializerPropertyTests.cs](./Property/FileMaterializerPropertyTests.cs) |

## 3.1 Formatbezogene Testabdeckung (Stand)
| Format | Detection | Validate | Extract Memory | Extract Disk |
|---|---|---|---|---|
| ZIP | abgedeckt | abgedeckt | abgedeckt | abgedeckt |
| TAR | abgedeckt | indirekt ueber Unified-Pipeline | implizit ueber Unified-Pipeline | implizit ueber Unified-Pipeline |
| TAR.GZ | abgedeckt | abgedeckt | abgedeckt | abgedeckt |
| 7z | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) |
| RAR | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) | abgedeckt (BDD Byte-Array Flow) |

## 4. Ausfuehrung
Vollstaendige Ausfuehrung (ohne Filter, fuehrt alle Tests aus):
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
  - `fixtureId` (kanonischer, stabiler Identifier; bevorzugt fuer Referenzen),
  - `dataType` (fachliche Klassifikation),
  - `objectId` (`sha256:<hex>` als content-addressed Objekt-ID),
  - `sha256` (Integritaetsnachweis),
  - `sourceUrl`/`sourceRef` (Herkunft).
- Beim ersten Zugriff validiert `TestResources` den kompletten Ressourcenbestand fail-closed:
  - Hash-Mismatch, fehlende Manifest-Eintraege oder unregistrierte Dateien => Testfehler.

## 5. Tag-Filter (Reqnroll-Annotationen)
Filter werden ueber `--filter "Category=<tag>"` (ohne `@`) als Suffix angehaengt.
Ohne Filter-Suffix werden immer alle Tests ausgefuehrt.
Basisbefehl fuer alle Tabellen unten:

```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal
```

### 5.1 Annotationen nach Testtyp
| Annotation | Bedeutung | Filter-Suffix (nur diese Tests) |
|---|---|---|
| `@unit` | BDD-Tests mit unit-nahem Fokus | `--filter "Category=unit"` |
| `@integration` | BDD-Integrationsfluesse zwischen Komponenten | `--filter "Category=integration"` |
| `@e2e` | End-to-End-Fluesse inkl. Dateisystempfaden | `--filter "Category=e2e"` |
| `@positiv` | erwartete Erfolgsfaelle | `--filter "Category=positiv"` |
| `@negativ` | fail-closed-, Fehler- und Grenzfaelle | `--filter "Category=negativ"` |

### 5.2 Annotationen nach Klasse/Komponente
| Annotation | Bedeutung | Filter-Suffix (nur diese Tests) |
|---|---|---|
| `@detector` | Faelle um `FileTypeDetector` | `--filter "Category=detector"` |
| `@materializer` | Faelle um `FileMaterializer` | `--filter "Category=materializer"` |
| `@processing` | Faelle um Pipeline/Fassaden (`ZipProcessing`) | `--filter "Category=processing"` |
| `@zip` | ZIP-bezogene Pfade (Validate/Refine/Extract) | `--filter "Category=zip"` |
| `@api` | API-orientierte BDD-Faelle | `--filter "Category=api"` |

### 5.3 Annotationen nach Fachthema
| Annotation | Bedeutung | Filter-Suffix (nur diese Tests) |
|---|---|---|
| `@detektion` | Signatur-/Typdetektion | `--filter "Category=detektion"` |
| `@endung` | Endungs-Policy-Faelle | `--filter "Category=endung"` |
| `@lesen` | sichere Datei-zu-Bytes-Lesepfade | `--filter "Category=lesen"` |
| `@typpruefung` | `IsOfType`-Pruefpfade | `--filter "Category=typpruefung"` |
| `@grenzen` | MaxBytes-/Grenzwertpfade | `--filter "Category=grenzen"` |
| `@konfiguration` | Build-/Konfigurationspfade | `--filter "Category=konfiguration"` |
| `@refinement` | OOXML/ZIP-Refinement-Faelle | `--filter "Category=refinement"` |
