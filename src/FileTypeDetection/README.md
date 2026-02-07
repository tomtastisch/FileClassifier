# FileTypeDetection Modul

## 1. Zweck
Dieses Verzeichnis stellt die öffentliche Bibliotheksoberfläche für Dateityperkennung, sichere Archivverarbeitung, deterministische Hashing-Nachweise und Byte-Materialisierung bereit.

## 2. Inhalt
- Öffentliche API-Einstiegspunkte: `FileTypeDetector`, `ArchiveProcessing`, `FileMaterializer`, `FileTypeOptions`, `DeterministicHashing`.
- Submodule für Modellklassen, Registry/Detection, Konfiguration und Infrastruktur.

## 3. API und Verhalten
- `FileTypeDetector`: Typdetektion aus Pfad/Bytes, Detailnachweise und sichere Archivpfade.
- `ArchiveProcessing`: statische Fassade für Validierung/Extraktion.
- `FileMaterializer`: persistiert Byte-Payloads, optional sichere Archiv-Materialisierung.
- `DeterministicHashing`: Physical/Logical Hash-Evidence und RoundTrip-Reports.

## 4. Verifikation
- Unit/Integration/BDD-Nachweise liegen unter dem Testprojekt.
- Dokumentations- und Link-Gates laufen über die zentralen Tools.

## 5. Diagramm
```mermaid
flowchart LR
    A[Consumer Input] --> B[Public API]
    B --> C[Detection and Archive Safety]
    C --> D[Typed Result or Persisted Output]
```

## 6. Verweise
- [Dokumentationsindex](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/001_INDEX_CORE.MD)
- [API-Kernübersicht](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/010_API_CORE.MD)
- [Architektur und Flows](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/020_ARCH_CORE.MD)
- [Detektion-Submodul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Detection/README.md)
- [Infrastruktur-Submodul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Infrastructure/README.md)
- [Konfiguration-Submodul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Configuration/README.md)
- [Abstractions-Submodul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Abstractions/README.md)
