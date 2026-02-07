# Abstractions Archive Modul

## 1. Zweck
Dieses Verzeichnis stellt das einheitliche In-Memory-Archiv-Entry-Modell bereit.

## 2. Inhalt
- `ZipExtractedEntry.vb`

## 3. API und Verhalten
- Modellname bleibt aus Kompatibilitätsgründen historisch.
- Semantik ist archivformat-generisch für die einheitliche Pipeline.

## 4. Verifikation
- Unit-Tests prüfen Konstruktion, Stream-Zugriff und Invarianten.

## 5. Diagramm
```mermaid
flowchart LR
    A[Archive Extractor] --> B[ZipExtractedEntry]
    B --> C[Consumer Processing]
```

## 6. Verweise
- [Abstractions-Übersicht](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)
- [Architektur und Flows](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
