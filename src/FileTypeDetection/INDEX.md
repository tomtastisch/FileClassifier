# Index - src/FileTypeDetection

## 1. Ziel dieses Moduls
Deterministische Dateityp-Erkennung und sichere ZIP-Verarbeitung mit fail-closed Verhalten.

## 2. Schnellstart fuer Leser
1. [API-Referenz](./docs/API_REFERENCE.md)
2. [DIN-orientierte Spezifikation](./docs/DIN_SPECIFICATION_DE.md)
3. [Detection-Details](./Detection/INDEX.md)
4. [Infrastructure-Details](./Infrastructure/INDEX.md)

## 3. Strukturregel (wichtig)
Im Modul-Root liegen nur oeffentliche API-Einstiegspunkte:
- [FileTypeDetector.vb](./FileTypeDetector.vb)
- [ZipProcessing.vb](./ZipProcessing.vb)
- [FileMaterializer.vb](./FileMaterializer.vb)
- [FileTypeOptions.vb](./FileTypeOptions.vb)

Alle Low-Level-Implementierungen liegen in Unterordnern.

## 4. Ordner und Verantwortungen
| Pfad | Verantwortung | Typische Leser |
|---|---|---|
| [Abstractions/](./Abstractions/INDEX.md) | Immutable Rueckgabemodelle | API-Consumer |
| [Configuration/](./Configuration/INDEX.md) | Optionen, Security-Baseline | Ops, Security, Entwickler |
| [Detection/](./Detection/INDEX.md) | SSOT fuer Typen, Aliase, Header-Magic | Maintainer Detection |
| [Infrastructure/](./Infrastructure/INDEX.md) | ZIP-Gate, Refiner, Extractor, Bounds | Maintainer Security/IO |

## 5. Architekturdiagramm
```mermaid
flowchart LR
    CFG[Configuration]--> API[Root API]
    API --> DET[Detection]
    API --> INF[Infrastructure]
    DET --> ABS[Abstractions]
    INF --> ABS
```

## 6. Oeffentliche Funktionen (Uebersicht)
| Klasse | Funktionale Rolle | Detailtabelle |
|---|---|---|
| `FileTypeDetector` | Erkennung, Policy, ZIP-Path-Operationen | [docs/API_REFERENCE.md](./docs/API_REFERENCE.md) |
| `ZipProcessing` | statische ZIP-Fassade (Path/Bytes) | [docs/API_REFERENCE.md](./docs/API_REFERENCE.md) |
| `FileMaterializer` | einheitliche Persistenz fuer Byte-Payloads (optional ZIP->Disk) | [docs/API_REFERENCE.md](./docs/API_REFERENCE.md) |
| `FileTypeOptions` | zentrale JSON-Optionsschnittstelle (laden/lesen) | [docs/API_REFERENCE.md](./docs/API_REFERENCE.md) |
| `FileTypeSecurityBaseline` | konservative Security-Defaults | [Configuration/INDEX.md](./Configuration/INDEX.md) |

## 7. Qualitaetsziele (ISO/IEC 25010)
- Functional suitability: korrektes Mapping Header/Container -> `FileKind`.
- Reliability: fail-closed bei Fehlerpfaden und Grenzverletzungen.
- Security: ZIP-Traversal/Bomb-Schutz, kein Endungsvertrauen.
- Maintainability: Root-API klein, interne Verantwortungen getrennt.

## 8. Pflichtdiagramme fuer Entwickler
```mermaid
flowchart TD
    A[Architekturueberblick] --> B[Sicherheitsfluss ZIP-Gate]
    B --> C[Kritischer Sequenzpfad: Validate/Extract]
```

## 9. NuGet-/Framework-Abhaengigkeiten (Uebersicht)
```mermaid
flowchart LR
    API[Public APIs] --> CORE[Infrastructure]
    CORE --> ZIP[System.IO.Compression]
    CORE --> MIME[Mime]
    CORE --> RMS[Microsoft.IO.RecyclableMemoryStream]
    API --> SHARP[SharpCompress]
    API --> LOG[Microsoft.AspNetCore.App -> Logging]
```

## 10. Nachweise
- Build: `dotnet build FileClassifier.sln --no-restore -v minimal`
- Test: `dotnet test FileClassifier.sln --no-build -v minimal`
- Portable Check: `bash tools/check-portable-filetypedetection.sh --clean`
