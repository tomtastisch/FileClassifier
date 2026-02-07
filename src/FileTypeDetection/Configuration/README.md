# Configuration Modul

## 1. Zweck
Dieses Verzeichnis definiert die deterministische Laufzeitkonfiguration und konservative Sicherheits-Baselines.

## 2. Inhalt
- `FileTypeProjectOptions.vb`.
- `FileTypeProjectBaseline.vb`.

## 3. API und Verhalten
- Steuert globale Grenzen und Schalter für Detection, Archive und Hashing.
- Baseline setzt reproduzierbare Defaultwerte für produktive Nutzung.

## 4. Verifikation
- Unit-Tests prüfen Normalisierung, Snapshot-Verhalten und Baseline-Konsistenz.

## 5. Diagramm
```mermaid
flowchart LR
    A[Config JSON or Baseline] --> B[Options Normalization]
    B --> C[Global Snapshot]
```

## 6. Verweise
- [Modulübersicht](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/README.md)
- [API-Kernübersicht](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/010_API_CORE.MD)
- [Options-Guide](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/guides/001_GUIDE_OPTIONS.MD)
