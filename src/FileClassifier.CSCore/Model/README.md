# CSCore Model Modul

## 1. Zweck
Immutable Datentraeger fuer projektionierte, serialisierbare und deterministic verarbeitbare Werte.

## 2. Inhalt
- `DetectionSignal.cs`: Eingabesignal fuer Detection-Projektionen.
- `DetectionSummary.cs`: normalisierte Detection-Sicht inkl. `HasStructuredMime`.
- `ProjectOptionsSnapshot.cs`: normalisierte Snapshot-Sicht fuer Projektoptionen.
- `HashOptionsSnapshot.cs`: normalisierte Snapshot-Sicht fuer Hash-Optionen.

## 3. Designregeln
- `record`-basierte Datentraeger mit Init-Only Semantik.
- Null-werte werden auf sichere Defaults normalisiert.
- Keine Seiteneffekte; reine Datenrepräsentation.

## 4. Verweise
- [CSCore Root](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.CSCore/README.md)
- [Mapping Layer](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.CSCore/Mapping/README.md)
