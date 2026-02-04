# Index - Configuration

## 1. Zweck
Deterministische Konfiguration der oeffentlichen API inklusive Sicherheitsbaseline.

## 2. Dateien
- [FileTypeDetectorOptions.vb](./FileTypeDetectorOptions.vb)
- [FileTypeSecurityBaseline.vb](./FileTypeSecurityBaseline.vb)

## 3. Optionen (wann relevant)
| Option | Wirkung | Typischer Trigger |
|---|---|---|
| `MaxBytes` | maximale Dateigroesse fuer Detect/Read | Upload-Limits, DoS-Schutz |
| `SniffBytes` | Header-Laenge fuer Magic-Pruefung | Dateiformate mit spaetem Marker |
| `MaxZipEntries` | Begrenzung Entry-Anzahl | ZIP-Bomb/Many-entry Schutz |
| `MaxZipTotalUncompressedBytes` | Gesamtgrenze ZIP | Speicher-/CPU-Schutz |
| `MaxZipEntryUncompressedBytes` | pro-Entry-Grenze | grosse Einzeldateien abfangen |
| `MaxZipCompressionRatio` | Kompressionsratio-Limit | stark komprimierte Bomben |
| `MaxZipNestingDepth` | maximale ZIP-Verschachtelung | rekursive Angriffe begrenzen |
| `MaxZipNestedBytes` | Nested-ZIP Byte-Limit | Memory-Schutz bei Nested-Content |
| `HeaderOnlyNonZip` | Header-only fuer Nicht-ZIP-Typen | konsistente Erkennungsstrategie |

## 4. Baseline-Strategie
- `FileTypeSecurityBaseline.ApplyDeterministicDefaults()` setzt konservative Werte fuer produktive Umgebungen.
- `FileTypeOptions.LoadOptions(json)` setzt Optionen via JSON (partiell, default-basiert).

## 5. Diagramm: Konfigurationsfluss
```mermaid
flowchart LR
    A[Startup] --> B[ApplyDeterministicDefaults]
    B --> C[LoadOptions]
    C --> D[Detect/Extract Laufzeit]
```

## 6. Testverknuepfung
- [FileTypeSecurityBaselineUnitTests.cs](../../../tests/FileTypeDetectionLib.Tests/Unit/FileTypeSecurityBaselineUnitTests.cs)
