# Index - tests/FileTypeDetectionLib.Tests

## 1. Zweck
Nachweis fuer Sicherheit, Determinismus, Korrektheit und API-Klarheit.

## 2. Testkategorien
| Kategorie | Fokus | Referenz |
|---|---|---|
| Unit | API- und Regelverhalten | [Unit/README.md](./Unit/README.md) |
| Property | Grenz-/Invarianztests fuer ZIP-Gate, Optionen und Materializer | [Property/README.md](./Property/README.md) |
| Features (BDD) | fachliche Lesbarkeit und Akzeptanz | [Features/README.md](./Features/README.md) |
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
```bash
dotnet test FileClassifier.sln --no-build -v minimal
```
