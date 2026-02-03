# D1) DEPENDENCY_RULES.md

## Harte Abhaengigkeitsregeln
1. `FileClassifier.App` darf nur auf oeffentliche APIs aus `src/FileTypeDetectionLib` zugreifen.
2. `FileTypeDetector` darf auf Detection/Zip/Infrastructure zugreifen, aber keine Testmodule referenzieren.
3. `ZipSafetyGate` und `ZipExtractor` duerfen nur ueber `ZipProcessingEngine` auf ZIP-Entry-Iteration zugreifen.
4. `ZipProcessingEngine` darf keine Kenntnis von CLI/App haben.
5. `FileTypeRegistry` bleibt einzige Quelle fuer Typ-/Aliasdaten (SSOT).
6. Portable-Dateien werden nicht manuell divergierend gepflegt; nur per Sync-Skript erzeugt.

## Konventionsregeln
- Eine Root-`README.md` pro Modul-Root (`src/FileTypeDetectionLib`, `tests/...`, `portable/...`).
- Unterordner: nur `INDEX.md`.
- Sicherheitsregeln in Code: fail-closed, bounded copy, path traversal checks.

## Beispiele
- Erlaubt: `FileTypeDetector -> ZipExtractor -> ZipProcessingEngine`.
- Nicht erlaubt: `FileTypeDetector` mit eigener zweiter ZIP-Entry-Schleife.
- Nicht erlaubt: direkte Typ-/Aliaslisten ausserhalb `FileTypeRegistry`.
