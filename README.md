# FileClassifier

## 1. Zielbild
Professionelle, auditierbare und deterministische Dateityp-Erkennung mit sicherer ZIP-Verarbeitung.

## 2. Kernprinzipien
- **fail-closed:** Fehlerpfade liefern `Unknown`, `False` oder leere Ergebnisse.
- **deterministisch:** gleiche Eingabe, gleiche Entscheidung.
- **kleine Public Surface:** nur wenige, klare Einstiegspunkte.
- **Security first:** ZIP-Gate, Traversal-Schutz, Groessen- und Rekursionslimits.

## 3. Architektur-Navigation
- Modulindex: [src/FileTypeDetection/INDEX.md](src/FileTypeDetection/INDEX.md)
- API-Referenz (vollstaendig): [src/FileTypeDetection/API_REFERENCE.md](src/FileTypeDetection/API_REFERENCE.md)
- Ablauf/UML: [src/FileTypeDetection/README.md](src/FileTypeDetection/README.md#ablauf--prozess--und-uml-dokumentation)
- Portable Erklaerung: [portable/README.md](portable/README.md)

## 4. API-Einstiegspunkte
- [src/FileTypeDetection/FileTypeDetector.vb](src/FileTypeDetection/FileTypeDetector.vb)
- [src/FileTypeDetection/ZipProcessing.vb](src/FileTypeDetection/ZipProcessing.vb)

## 5. Traceability (Ziel -> Check -> Evidenz)
| Ziel | Check | Evidenz |
|---|---|---|
| Determinismus | Typ-/Alias-Mapping stabil | [FileTypeRegistryUnitTests.cs](tests/FileTypeDetectionLib.Tests/Unit/FileTypeRegistryUnitTests.cs) |
| Fail-closed | Adversarial ZIPs werden abgewiesen | [ZipAdversarialTests.cs](tests/FileTypeDetectionLib.Tests/Unit/ZipAdversarialTests.cs) |
| Sichere Extraktion | Traversal/Kollision/Nesting abgefangen | [ZipExtractionUnitTests.cs](tests/FileTypeDetectionLib.Tests/Unit/ZipExtractionUnitTests.cs) |
| API-Klarheit | Audit-Details + ZIP-Fassade | [DetectionDetailAndZipValidationUnitTests.cs](tests/FileTypeDetectionLib.Tests/Unit/DetectionDetailAndZipValidationUnitTests.cs), [ZipProcessingFacadeUnitTests.cs](tests/FileTypeDetectionLib.Tests/Unit/ZipProcessingFacadeUnitTests.cs) |

## 6. Runbook (reproduzierbar)
```bash
dotnet restore FileClassifier.sln -v minimal
dotnet build FileClassifier.sln --no-restore -v minimal
dotnet test FileClassifier.sln --no-build -v minimal
bash tools/sync-portable-filetypedetection.sh
bash tools/check-portable-filetypedetection.sh --clean
```

## 7. Aktueller Strukturzustand
### 7.1 Source
Im Root von `src/FileTypeDetection` liegen nur die Public APIs.

### 7.2 Portable
Im Root von `portable/FileTypeDetection` liegen nur die Public APIs, alle anderen Klassen sind in Unterordnern strukturiert.
