# Detection Modul

## 1. Zweck
Dieses Verzeichnis ist die SSOT für Header-Magic, Aliasauflösung und kanonische Typzuordnung.

## 2. Inhalt
- `FileTypeRegistry.vb` mit Typdefinitionen, Alias-Mapping und Magic-Pattern-Katalog.

## 3. API und Verhalten
- Liefert deterministische Zuordnung `Header -> FileKind`.
- Stellt fail-closed Fallback für unbekannte Eingaben sicher.
- Unterstützt Aliasnormalisierung ohne duplizierte Consumer-Logik.

## 4. Verifikation
- Unit-Tests prüfen Mapping, Aliasregeln und Header-Coverage-Policy.

## 5. Diagramm
```mermaid
flowchart LR
    A[Header Bytes] --> B[Registry Match]
    B --> C[Resolved FileKind]
```

## 6. Verweise
- [Modulübersicht](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [API-Kernübersicht](https://github.com/tomtastisch/FileClassifier/blob/main/docs/010_API_CORE.MD)
- [Architektur und Flows](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
