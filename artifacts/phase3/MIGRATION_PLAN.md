# D2) MIGRATION_PLAN.md

## Sequenz (deterministisch, minimal-invasiv)

### Schritt 1: Architektur-Schnitt fuer ZIP intern vorbereiten
- Aktion:
  - Neue Datei `src/FileTypeDetectionLib/Infrastructure/ZipProcessing.vb` anlegen.
  - `ZipProcessingEngine.ProcessZipStream(...)` aus `ZipSafetyGate.ProcessZipStream` extrahieren.
- Akzeptanz:
  - Build + Tests gruen.
  - Keine Verhaltensaenderung in ZIP-Tests.

### Schritt 2: Adapter trennen
- Aktion:
  - `ZipSafetyGate` auf `ZipProcessingEngine` umstellen (nur Validate-Pfad).
  - Neue interne Klasse `ZipExtractor` anlegen (nur Extract-Pfad, nutzt Engine).
- Akzeptanz:
  - `ZipExtractionUnitTests` und `ZipGatePropertyTests` unveraendert gruen.
  - Keine doppelte Entry-Loop im Repo.

### Schritt 3: API-Routing sauberstellen
- Aktion:
  - `FileTypeDetector.ExtractZipSafe` von `ZipSafetyGate.TryExtractZipStream` auf `ZipExtractor.TryExtractZipStream` umstellen.
- Akzeptanz:
  - Gleiche oeffentliche API-Signatur.
  - CLI/Tests zeigen identisches Verhalten.

### Schritt 4: Doku + Portable Sync
- Aktion:
  - `tools/sync-portable-filetypedetection.sh` updaten, portable neu generieren.
  - betroffene INDEX/README klarstellen (neue interne Modulgrenzen).
- Akzeptanz:
  - `src` und `portable` Code-Dateien hash-identisch.
  - Doku-Regeln (1 Root README + Unterordner INDEX) eingehalten.

### Schritt 5: Abschlussvalidierung
- Aktion:
  - `dotnet build FileClassifier.sln -v minimal`
  - `dotnet test FileClassifier.sln -v minimal`
  - `bash tools/check-portable-filetypedetection.sh --clean`
- Akzeptanz:
  - Alle Kommandos Exit 0 (NU1900 nur als dokumentierte Warnung toleriert).
