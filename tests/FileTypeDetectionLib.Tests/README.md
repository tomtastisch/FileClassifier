# Index - tests/FileTypeDetectionLib.Tests

## 1. Zweck
Nachweis fuer Sicherheit, Determinismus, Korrektheit und API-Klarheit.

## 2. Testkategorien
| Kategorie | Fokus | Referenz |
|---|---|---|
| Unit | API- und Regelverhalten | [Unit/README.md](./Unit/README.md) |
| Property | Grenz-/Invarianztests fuer ZIP-Gate, Optionen und Materializer | [Property/README.md](./Property/README.md) |
| Features (BDD Unit/Integration/E2E) | fachliche Lesbarkeit und End-to-End-Flows | [Features/README.md](./Features/README.md) |
| Benchmarks | regressionsorientierte Laufzeittrends | [Benchmarks/README.md](./Benchmarks/README.md) |

## 3. Nachweismatrix
| Qualitaetsziel | Testdatei |
|---|---|
| Fail-closed ZIP-Grenzen | [Unit/ZipAdversarialTests.cs](./Unit/ZipAdversarialTests.cs) |
| Sichere ZIP-Extraktion | [Unit/ZipExtractionUnitTests.cs](./Unit/ZipExtractionUnitTests.cs) |
| Deterministische Registry | [Unit/FileTypeRegistryUnitTests.cs](./Unit/FileTypeRegistryUnitTests.cs) |
| API Detail-/ZIP-Fassade | [Unit/DetectionDetailAndZipValidationUnitTests.cs](./Unit/DetectionDetailAndZipValidationUnitTests.cs), [Unit/ZipProcessingFacadeUnitTests.cs](./Unit/ZipProcessingFacadeUnitTests.cs) |
| Options-/Materializer-Invarianten | [Property/FileTypeOptionsPropertyTests.cs](./Property/FileTypeOptionsPropertyTests.cs), [Property/FileMaterializerPropertyTests.cs](./Property/FileMaterializerPropertyTests.cs) |

## 4. Ausfuehrung
Vollstaendige Ausfuehrung (ohne Filter, fuehrt alle Tests aus):
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal
```

Alternative auf Solution-Ebene:
```bash
dotnet test FileClassifier.sln --no-build -v minimal
```

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
