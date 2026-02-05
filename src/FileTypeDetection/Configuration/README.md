# Index - Configuration

## 1. Zweck
Deterministische Konfiguration der oeffentlichen API inklusive Sicherheitsbaseline.

## 2. Dateien
- [FileTypeProjectOptions.vb](./FileTypeProjectOptions.vb)
- [FileTypeProjectBaseline.vb](./FileTypeProjectBaseline.vb)

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
| `DeterministicHash` | Default-Policy fuer Hash-Evidence (`IncludeFastHash`, `IncludePayloadCopies`, `MaterializedFileName`) | reproduzierbare h1-h4 Nachweise |

## 4. Baseline-Strategie
- `FileTypeProjectBaseline.ApplyDeterministicDefaults()` setzt konservative Werte fuer produktive Umgebungen.
- `FileTypeOptions.LoadOptions(json)` setzt Optionen via JSON (partiell, default-basiert).

## 5. Diagramm: Konfigurationsfluss
```mermaid
flowchart LR
    A[Startup] --> B[ApplyDeterministicDefaults]
    B --> C[LoadOptions]
    C --> D[Detect/Extract Laufzeit]
```

## 6. Testverknuepfung
- [FileTypeProjectBaselineUnitTests.cs](../../../tests/FileTypeDetectionLib.Tests/Unit/FileTypeProjectBaselineUnitTests.cs)

## 7. Siehe auch
- [Modulindex](../README.md)
- [Funktionsreferenz](../../../docs/01_FUNCTIONS.md)
- [Architektur und Ablaufe](../../../docs/02_ARCHITECTURE_AND_FLOWS.md)
