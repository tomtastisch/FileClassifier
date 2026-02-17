# Core Modul

## 1. Zweck
Dieses Verzeichnis reserviert den Raum fuer gemeinsame, TFM-unabhaengige Kernlogik ohne direkte TFM-spezifische API-Aufrufe.

## 2. Inhalt
- Derzeit nur Strukturanker fuer netstandard2.0-kompatible Refactorings.

## 3. API und Verhalten
- Core-Code darf keine direkten Aufrufe auf moderne TFM-spezifische APIs enthalten.
- TFM-sensitive Primitive werden ausschliesslich ueber Provider-Abstraktionen konsumiert.

## 4. Verifikation
- Nachweis erfolgt ueber Grep-Checks und Build-Matrix je TFM.

## 5. Diagramm
N/A

## 6. Verweise
- [Moduluebersicht](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Abstractions Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)
- [Providers Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Providers/README.md)
