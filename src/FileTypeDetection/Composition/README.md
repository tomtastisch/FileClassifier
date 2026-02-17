# Composition Modul

## 1. Zweck
Dieses Verzeichnis kapselt den einzigen Kompositionspunkt für interne Hashing-Provider.

## 2. Inhalt
- `HashPrimitives.vb`

## 3. API und Verhalten
- `HashPrimitives.Current` liefert den compile-time gebundenen Provider.
- Kein Runtime-Branching, keine Environment-Erkennung, keine DI-basierte Provider-Umschaltung.

## 4. Verifikation
- Reflection-Tests prüfen die Verfügbarkeit und den Provider-Marker.

## 5. Diagramm
```mermaid
flowchart LR
    A["EvidenceHashing"] --> B["HashPrimitives.Current"]
    B --> C["HashPrimitivesProvider (compiled for TFM)"]
```

## 6. Verweise
- [Abstractions Providers Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Providers/README.md)
- [Providers Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Providers/README.md)
