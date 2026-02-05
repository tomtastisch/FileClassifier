# Index - src/FileTypeDetection

## 1. Ziel dieses Moduls
Deterministische Dateityp-Erkennung und sichere Archiv-Verarbeitung (u. a. ZIP/TAR/GZIP/7z/RAR) mit fail-closed Verhalten.

## 2. Schnellstart fuer Leser
1. [Doku-Index](../../docs/README.md)
2. [Funktionen](../../docs/01_FUNCTIONS.md)
3. [Architektur und Ablaufe](../../docs/02_ARCHITECTURE_AND_FLOWS.md)
4. [Referenzen](../../docs/03_REFERENCES.md)
5. [Production Readiness Checklist](../../docs/PRODUCTION_READINESS_CHECKLIST.md)
6. [DIN-orientierte Spezifikation](../../docs/DIN_SPECIFICATION_DE.md)
7. [Detection-Details](./Detection/README.md)
8. [Infrastructure-Details](./Infrastructure/README.md)
9. [Configuration-Details](./Configuration/README.md)
10. [Abstractions-Details](./Abstractions/README.md)

## 2.1 Empfohlene Lesepfade
- API-Nutzung zuerst: [docs/01_FUNCTIONS.md](../../docs/01_FUNCTIONS.md) -> [docs/02_ARCHITECTURE_AND_FLOWS.md](../../docs/02_ARCHITECTURE_AND_FLOWS.md) -> [docs/03_REFERENCES.md](../../docs/03_REFERENCES.md)
- Implementierungsdetails zu Flows: [Detection/README.md](./Detection/README.md) + [Infrastructure/README.md](./Infrastructure/README.md)
- Konfigurations- und Modellsicht: [Configuration/README.md](./Configuration/README.md) + [Abstractions/README.md](./Abstractions/README.md)

## 2.2 API-Semantikhinweis (wichtig)
- `TryValidateZip(...)` und `ZipProcessing.*` sind **historische API-Namen**.
- Die aktuelle Semantik ist container-generisch: validiert/extrahiert werden intern alle unterstuetzten Archive (ZIP/TAR/GZIP/7z/RAR) fail-closed.
- Grund: Public API bleibt stabil (kein Signaturbruch), interne Pipeline wurde auf Unified-Backend umgestellt.
- Verbindliche Details stehen in `docs/01_FUNCTIONS.md`:
  - Abschnitt "API-Wahrheit vs. historischer Name"
  - Abschnitt "Security-Gate Mini-Contract (neutral)"
  - Abschnitt "Formatmatrix (implementierte Semantik)"

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
| [Abstractions/](./Abstractions/README.md) | Immutable Rueckgabemodelle | API-Consumer |
| [Configuration/](./Configuration/README.md) | Optionen, Security-Baseline | Ops, Security, Entwickler |
| [Detection/](./Detection/README.md) | SSOT fuer Typen, Aliase, Header-Magic | Maintainer Detection |
| [Infrastructure/](./Infrastructure/README.md) | ZIP-Gate, Refiner, Extractor, Bounds | Maintainer Security/IO |

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
| `FileTypeDetector` | Erkennung, Policy, ZIP-Path-Operationen | [docs/01_FUNCTIONS.md](../../docs/01_FUNCTIONS.md) |
| `ZipProcessing` | statische Archiv-Fassade (Path/Bytes), historisch als ZIP benannt | [docs/01_FUNCTIONS.md](../../docs/01_FUNCTIONS.md) |
| `FileMaterializer` | einheitliche Persistenz fuer Byte-Payloads (optional ZIP->Disk) | [docs/01_FUNCTIONS.md](../../docs/01_FUNCTIONS.md) |
| `FileTypeOptions` | zentrale JSON-Optionsschnittstelle (laden/lesen) | [docs/01_FUNCTIONS.md](../../docs/01_FUNCTIONS.md) |
| `FileTypeSecurityBaseline` | konservative Security-Defaults | [Configuration/README.md](./Configuration/README.md) |

## 7. Qualitaetsziele (ISO/IEC 25010)
- Functional suitability: korrektes Mapping Header/Container -> `FileKind`.
- Reliability: fail-closed bei Fehlerpfaden und Grenzverletzungen.
- Security: Archiv-Traversal/Bomb-Schutz, kein Endungsvertrauen.
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
    CORE --> SHARP[SharpCompress]
    CORE --> MIME[Mime]
    CORE --> RMS[Microsoft.IO.RecyclableMemoryStream]
    API --> LOG[Microsoft.AspNetCore.App -> Logging]
```

## 10. Nachweise
- Build: `dotnet build FileClassifier.sln --no-restore -v minimal`
- Test: `dotnet test FileClassifier.sln --no-build -v minimal`
- Portable Check: `bash tools/check-portable-filetypedetection.sh --clean`
