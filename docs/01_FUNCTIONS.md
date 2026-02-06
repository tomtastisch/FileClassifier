# 01 - Funktionen

## 1. Zweck & Scope
Dieses Dokument beschreibt alle öffentlichen Einstiegspunkte der API mit Signaturen, Einsatzfall, Seiteneffekten und minimalen Aufrufbeispielen.

## 2. Definitionen
- Fail-closed: Fehlerpfade liefern nur sichere Rückgaben (`Unknown`, `False`, leere Liste).
- Side-Effects: Dateisystemschreibvorgänge oder globale Optionsänderungen.
- Flow-ID: Verweis auf Architekturabläufe in `02_ARCHITECTURE_AND_FLOWS.md`.

## 2.1 API-Semantik (prominent)
- `TryValidateArchive(...)` ist die kanonische Validierungs-API.
- Die Methode validiert **alle intern unterstützten Archivformate** fail-closed (u. a. ZIP/TAR/GZIP/7z/RAR).
- Technisch passiert die Typfeststellung über `ArchiveTypeResolver` + `ArchiveSafetyGate`; OOXML-Refinement bleibt container-spezifisch für ZIP-basierte OOXML-Dateien.
- Begriffsklärung: `ContainerType` bezeichnet das **physische Archivformat** (z. B. ZIP/TAR/GZIP/7z/RAR), während `FileKind.Zip` aus Kompatibilitätsgründen der logische Archiv-Rückgabetyp bleibt.

## 2.2 Weiterführende Detailquellen pro Familie
| API-Familie | Detailquelle | Zweck |
|---|---|---|
| `FileTypeDetector` / `ArchiveProcessing` | [`../src/FileTypeDetection/Detection/README.md`](../src/FileTypeDetection/Detection/README.md) | SSOT-Detektion, Header-Magic, Aliaslogik |
| `FileTypeDetector` / `ArchiveProcessing` / `FileMaterializer` | [`../src/FileTypeDetection/Infrastructure/README.md`](../src/FileTypeDetection/Infrastructure/README.md) | Archive-Gate, Guards, Extraktions-Engine |
| `FileTypeOptions` / `FileTypeProjectBaseline` | [`../src/FileTypeDetection/Configuration/README.md`](../src/FileTypeDetection/Configuration/README.md) | globale Optionen und Baseline |
| Rückgabemodelle (`FileType`, `DetectionDetail`, `ZipExtractedEntry`, `DeterministicHash*`) | [`../src/FileTypeDetection/Abstractions/README.md`](../src/FileTypeDetection/Abstractions/README.md) | Modellverträge der Public API |
| Modulnavigation | [`../src/FileTypeDetection/README.md`](../src/FileTypeDetection/README.md) | Übersicht und Einstieg je Leserrolle |

## 2.2.1 Physische Modellablage (ohne Funktionsänderung)
Die Rückgabemodelle sind rein organisatorisch in Unterordner aufgeteilt; die API-Semantik bleibt unverändert:
- Detection-Modelle: [`../src/FileTypeDetection/Abstractions/Detection/README.md`](../src/FileTypeDetection/Abstractions/Detection/README.md)
- Archive-Modelle: [`../src/FileTypeDetection/Abstractions/Archive/README.md`](../src/FileTypeDetection/Abstractions/Archive/README.md)
- Hashing-Modelle: [`../src/FileTypeDetection/Abstractions/Hashing/README.md`](../src/FileTypeDetection/Abstractions/Hashing/README.md)

## 2.3 Glossar (kanonische Begriffe)
| Begriff | Bedeutung |
|---|---|
| Archivformat | Physischer Container wie ZIP, TAR, TAR.GZ, 7z oder RAR. |
| ContainerType | Interner technischer Typ für das physische Archivformat. |
| FileKind.Zip | Öffentlicher, kompatibler logischer Rückgabetyp für erkannte Archive. |
| PhysicalHash | SHA-256 über die unveränderten Rohbytes einer Datei oder eines Byte-Arrays. |
| LogicalHash | SHA-256 über kanonisierten Inhaltszustand (Entry-Pfad + Inhalt), unabhängig von Containerdetails. |
| FastHash | Optionaler, nicht-kryptografischer Vergleichsdigest (`XxHash3`) für Performance-Shortcuts. |
| Fail-closed | Unsichere oder ungültige Eingaben liefern nur sichere Rückgaben (`Unknown`, `False`, leere Liste). |

## 3. Vollständige Methodenmatrix (Public API)
| Familie | Methode | Input | Output | Side-Effects | Primarer Flow |
|---|---|---|---|---|---|
| `FileTypeDetector` | `ReadFileSafe(path)` | Datei-Pfad | `Byte()` | keine (read-only) | `F0` |
| `FileTypeDetector` | `Detect(path)` | Datei-Pfad | `FileType` | keine | `F1` |
| `FileTypeDetector` | `Detect(path, verifyExtension)` | Datei-Pfad + Bool | `FileType` | keine | `F1` |
| `FileTypeDetector` | `DetectDetailed(path)` | Datei-Pfad | `DetectionDetail` | keine | `F1` |
| `FileTypeDetector` | `DetectDetailed(path, verifyExtension)` | Datei-Pfad + Bool | `DetectionDetail` | keine | `F1` |
| `FileTypeDetector` | `DetectAndVerifyExtension(path)` | Datei-Pfad | `Boolean` | keine | `F8` |
| `FileTypeDetector` | `TryValidateArchive(path)` | Datei-Pfad | `Boolean` | keine | `F3` |
| `FileTypeDetector` | `Detect(data)` | `Byte()` | `FileType` | keine | `F2` |
| `FileTypeDetector` | `IsOfType(data, kind)` | `Byte()` + `FileKind` | `Boolean` | keine | `F2` |
| `FileTypeDetector` | `ExtractArchiveSafe(path, destination, verifyBeforeExtract)` | Pfad + Ziel + Bool | `Boolean` | schreibt auf Disk | `F5` |
| `FileTypeDetector` | `ExtractArchiveSafeToMemory(path, verifyBeforeExtract)` | Pfad + Bool | `IReadOnlyList(Of ZipExtractedEntry)` | keine | `F4` |
| `ArchiveProcessing` | `TryValidate(path)` | Datei-Pfad | `Boolean` | keine | `F3` |
| `ArchiveProcessing` | `TryValidate(data)` | `Byte()` | `Boolean` | keine | `F3` |
| `ArchiveProcessing` | `ExtractToMemory(path, verifyBeforeExtract)` | Pfad + Bool | `IReadOnlyList(Of ZipExtractedEntry)` | keine | `F4` |
| `ArchiveProcessing` | `TryExtractToMemory(data)` | `Byte()` | `IReadOnlyList(Of ZipExtractedEntry)` | keine | `F4` |
| `FileMaterializer` | `Persist(data, destinationPath)` | `Byte()` + Zielpfad | `Boolean` | schreibt auf Disk | `F6` |
| `FileMaterializer` | `Persist(data, destinationPath, overwrite)` | `Byte()` + Zielpfad + Bool | `Boolean` | schreibt auf Disk | `F6` |
| `FileMaterializer` | `Persist(data, destinationPath, overwrite, secureExtract)` | `Byte()` + Zielpfad + 2 Bool | `Boolean` | schreibt auf Disk | `F5`/`F6` |
| `DeterministicHashing` | `HashFile(path)` | Datei-Pfad | `DeterministicHashEvidence` | keine | `F9` |
| `DeterministicHashing` | `HashFile(path, options)` | Datei-Pfad + Optionen | `DeterministicHashEvidence` | keine | `F9` |
| `DeterministicHashing` | `HashBytes(data)` | `Byte()` | `DeterministicHashEvidence` | keine | `F9` |
| `DeterministicHashing` | `HashBytes(data, label)` | `Byte()` + Label | `DeterministicHashEvidence` | keine | `F9` |
| `DeterministicHashing` | `HashBytes(data, label, options)` | `Byte()` + Label + Optionen | `DeterministicHashEvidence` | keine | `F9` |
| `DeterministicHashing` | `HashEntries(entries)` | `IReadOnlyList(Of ZipExtractedEntry)` | `DeterministicHashEvidence` | keine | `F9` |
| `DeterministicHashing` | `HashEntries(entries, label)` | Entries + Label | `DeterministicHashEvidence` | keine | `F9` |
| `DeterministicHashing` | `HashEntries(entries, label, options)` | Entries + Label + Optionen | `DeterministicHashEvidence` | keine | `F9` |
| `DeterministicHashing` | `VerifyRoundTrip(path)` | Datei-Pfad | `DeterministicHashRoundTripReport` | schreibt temp-Datei intern und räumt auf | `F9` |
| `DeterministicHashing` | `VerifyRoundTrip(path, options)` | Datei-Pfad + Optionen | `DeterministicHashRoundTripReport` | schreibt temp-Datei intern und räumt auf | `F9` |
| `FileTypeOptions` | `LoadOptions(json)` | JSON | `Boolean` | ändert globale Optionen | `F7` |
| `FileTypeOptions` | `GetOptions()` | - | `String` (JSON) | keine | `F7` |
| `FileTypeProjectBaseline` | `ApplyDeterministicDefaults()` | - | `Void` | ändert globale Optionen | `F7` |

## 4. Methodenfamilien
### 4.1 FileTypeDetector
Details: [`../src/FileTypeDetection/README.md`](../src/FileTypeDetection/README.md), [`../src/FileTypeDetection/Detection/README.md`](../src/FileTypeDetection/Detection/README.md), [`../src/FileTypeDetection/Infrastructure/README.md`](../src/FileTypeDetection/Infrastructure/README.md).

**Wichtiger Semantikhinweis:** `TryValidateArchive(path)` validiert generische Archivcontainer über dieselbe fail-closed Pipeline wie Extraktion und Materialisierung.

```mermaid
flowchart TD
    I0["Input: Path or Bytes"]
    D1["Detect / DetectDetailed"]
    D2["DetectAndVerifyExtension"]
    V1["TryValidateArchive"]
    X1["ExtractArchiveSafe / ExtractArchiveSafeToMemory"]
    O0["Output: Type / Detail / Bool / Entries"]

    I0 --> D1 --> O0
    I0 --> D2 --> O0
    I0 --> V1 --> O0
    I0 --> X1 --> O0
```

```csharp
using FileTypeDetection;

var detector = new FileTypeDetector();
var t = detector.Detect("/data/invoice.pdf", verifyExtension: true);
var d = detector.DetectDetailed("/data/archive.docx", verifyExtension: true);
bool archiveOk = detector.TryValidateArchive("/data/archive.zip");
var entries = detector.ExtractArchiveSafeToMemory("/data/archive.zip", verifyBeforeExtract: true);

Console.WriteLine($"{t.Kind} / {d.ReasonCode} / {archiveOk} / {entries.Count}");
```

### 4.2 ArchiveProcessing
API-Name bleibt aus Kompatibilitätsgründen erhalten; intern werden Archivcontainer einheitlich behandelt.
Details: [`../src/FileTypeDetection/README.md`](../src/FileTypeDetection/README.md), [`../src/FileTypeDetection/Infrastructure/README.md`](../src/FileTypeDetection/Infrastructure/README.md).

```mermaid
flowchart LR
    ZI["Input: Path or Bytes"]
    ZV["TryValidate(path|data)"]
    ZE["ExtractToMemory / TryExtractToMemory"]
    ZO["Output: Bool or Entries"]

    ZI --> ZV --> ZO
    ZI --> ZE --> ZO
```

```csharp
using FileTypeDetection;

bool okPath = ArchiveProcessing.TryValidate("/data/archive.zip");
bool okBytes = ArchiveProcessing.TryValidate(File.ReadAllBytes("/data/archive.zip"));
var entriesPath = ArchiveProcessing.ExtractToMemory("/data/archive.zip", verifyBeforeExtract: true);
var entriesBytes = ArchiveProcessing.TryExtractToMemory(File.ReadAllBytes("/data/archive.zip"));
```

### 4.3 FileMaterializer
Details: [`../src/FileTypeDetection/README.md`](../src/FileTypeDetection/README.md), [`../src/FileTypeDetection/Infrastructure/README.md`](../src/FileTypeDetection/Infrastructure/README.md).

```mermaid
flowchart TD
    MI["Input: Byte Payload + destinationPath"]
    MP["Persist(...)"]
    BR["secureExtract and archive?"]
    RW["Raw Byte Write"]
    ZE["Validated Archive Extract to Disk"]
    MO["Output: Bool"]

    MI --> MP --> BR
    BR -->|"No"| RW --> MO
    BR -->|"Yes"| ZE --> MO
```

```csharp
using FileTypeDetection;

byte[] payload = File.ReadAllBytes("/data/input.bin");
byte[] archivePayload = File.ReadAllBytes("/data/archive.zip");

bool rawOk = FileMaterializer.Persist(payload, "/data/out/input.bin", overwrite: false, secureExtract: false);
bool archiveExtractOk = FileMaterializer.Persist(archivePayload, "/data/out/unpacked", overwrite: false, secureExtract: true);
```

### 4.4 FileTypeOptions + FileTypeProjectBaseline
Details: [`../src/FileTypeDetection/README.md`](../src/FileTypeDetection/README.md), [`../src/FileTypeDetection/Configuration/README.md`](../src/FileTypeDetection/Configuration/README.md).

```mermaid
flowchart LR
    C0["Config Input: JSON or Baseline Call"]
    C1["LoadOptions(json)"]
    C2["ApplyDeterministicDefaults()"]
    C3["Global Snapshot"]
    C4["GetOptions()"]

    C0 --> C1 --> C3
    C0 --> C2 --> C3
    C3 --> C4
```

```csharp
using FileTypeDetection;

FileTypeProjectBaseline.ApplyDeterministicDefaults();
bool loaded = FileTypeOptions.LoadOptions("{\"maxBytes\":134217728}");
string snapshot = FileTypeOptions.GetOptions();
Console.WriteLine($"Loaded={loaded}; Snapshot={snapshot}");
```

### 4.5 DeterministicHashing
Details: [`../src/FileTypeDetection/README.md`](../src/FileTypeDetection/README.md), [`../src/FileTypeDetection/Abstractions/README.md`](../src/FileTypeDetection/Abstractions/README.md).
Formaler API-Contract: [`./04_DETERMINISTIC_HASHING_API_CONTRACT.md`](./04_DETERMINISTIC_HASHING_API_CONTRACT.md).

```mermaid
flowchart LR
    HI["Input: File/Bytes/Entries"]
    H1["Physical SHA-256 (raw bytes)"]
    H2["Logical SHA-256 (canonical content)"]
    H3["optional Fast XxHash3"]
    HR["RoundTrip h1-h4 report"]

    HI --> H1
    HI --> H2
    HI --> H3
    HI --> HR
```

```csharp
using FileTypeDetection;

var evidence = DeterministicHashing.HashFile("/data/archive.zip");
var report = DeterministicHashing.VerifyRoundTrip("/data/archive.zip");

Console.WriteLine($"{evidence.Digests.PhysicalSha256} / {evidence.Digests.LogicalSha256}");
Console.WriteLine($"LogicalConsistent={report.LogicalConsistent}");
```

Hinweis:
- `PhysicalSha256` und `LogicalSha256` sind Security-SSOT.
- `Fast*XxHash3` ist optionaler Performance-Digest und kein kryptografischer Integritätsnachweis.
- Overloads ohne `options` verwenden die globale Policy `FileTypeOptions.GetSnapshot().DeterministicHash`.

## 5. Nicht-Ziele
- Keine interne Low-Level-Implementierung im Detail (siehe `03_REFERENCES.md`).
- Keine Norm/Compliance-Herleitung (siehe `DIN_SPECIFICATION_DE.md`).

## 6. Security-Gate Mini-Contract (neutral)
Die folgenden Regeln gelten für `ArchiveSafetyGate` + `ArchiveExtractor`:

| Regel | Default | Vertrag |
|---|---|---|
| Link-Entries (`symlink`/`hardlink`) | `RejectArchiveLinks = true` | Link-Targets werden fail-closed verworfen. Override nur per explizitem Opt-In (`false`) und eigener Risikoentscheidung des Consumers. |
| Unknown Entry Size | `AllowUnknownArchiveEntrySize = false` | "Unknown" bedeutet `Size` nicht vorhanden oder negativ. Dann wird fail-closed geblockt bzw. per bounded Streaming gemessen; bei Grenzverletzung -> `False`. |
| Path-Sicherheit | aktiv | Entry-Name wird normalisiert (`\\` -> `/`), root/traversal/leer werden verworfen, Zielpfad muss Prefix-Check bestehen. |
| Grenzen | aktiv | Entry-Anzahl, per-Entry-Bytes, Gesamtbytes, Rekursionstiefe und archivformat-spezifische Ratio/Nested-Regeln bleiben fail-closed. |

## 7. Formatmatrix (implementierte Semantik)
Implementiert bedeutet hier: Archivformat wird geöffnet, Gate angewendet und Extraktion über dieselbe Pipeline ausgeführt.

| Format | Detection (`Detect`) | Validate (`TryValidateArchive` / `ArchiveProcessing.TryValidate`) | Extract-to-Memory (`ArchiveProcessing.TryExtractToMemory`) | Extract-to-Disk (`FileMaterializer.Persist(..., secureExtract:=True)`) |
|---|---|---|---|---|
| ZIP | Ja (Magic + Gate + optional OOXML-Refinement) | Ja | Ja | Ja |
| TAR | Ja (Container-Erkennung + Gate, logisches `FileKind.Zip`) | Ja | Ja | Ja |
| TAR.GZ | Ja (GZIP-Container, Nested-Archive-Pfad) | Ja | Ja | Ja |
| 7z | Ja (Container-Erkennung + Gate, logisches `FileKind.Zip`) | Ja | Ja | Ja |
| RAR | Ja (Container-Erkennung + Gate, logisches `FileKind.Zip`) | Ja | Ja | Ja |

Hinweis zur Typausgabe: In Phase 1 bleibt der öffentliche Rückgabetyp kompatibel; erkannte Archive werden als logischer `FileKind.Zip` gemeldet.

## 8. Nachweis Umwandlung/Absicherung + Testabdeckung
Die Byte-Pfade (Detect/Validate/Extract/Persist) sind auf die gleiche Gate-Pipeline gelegt.

| Format | Detection | Validate | Extract Memory | Extract Disk | Testnachweis |
|---|---|---|---|---|---|
| ZIP | abgedeckt | abgedeckt | abgedeckt | abgedeckt | `tests/FileTypeDetectionLib.Tests/Unit/ArchiveProcessingFacadeUnitTests.cs`, `tests/FileTypeDetectionLib.Tests/Unit/ArchiveExtractionUnitTests.cs`, `tests/FileTypeDetectionLib.Tests/Unit/FileMaterializerUnitTests.cs` |
| TAR | abgedeckt | indirekt über unified validate | implizit über Backend-Pfad | implizit über Backend-Pfad | `tests/FileTypeDetectionLib.Tests/Unit/UnifiedArchiveBackendUnitTests.cs` |
| TAR.GZ | abgedeckt | abgedeckt | abgedeckt | abgedeckt | `tests/FileTypeDetectionLib.Tests/Unit/UnifiedArchiveBackendUnitTests.cs` |
| 7z | abgedeckt | abgedeckt | abgedeckt | abgedeckt | `tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_040_ARCHIVE_TYPEN_BYTEARRAY_UND_MATERIALISIERUNG.feature` |
| RAR | abgedeckt | abgedeckt | abgedeckt | abgedeckt | `tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_040_ARCHIVE_TYPEN_BYTEARRAY_UND_MATERIALISIERUNG.feature` |

Zusatznachweis Security:
- Link-Entry fail-closed (`RejectArchiveLinks=true`): `tests/FileTypeDetectionLib.Tests/Unit/UnifiedArchiveBackendUnitTests.cs`.
- Archiv-Fail-closed Regressionen (Traversal, malformed Header, Overwrite/Target-Guards): `tests/FileTypeDetectionLib.Tests/Unit/FileMaterializerUnitTests.cs`.

Portabilität/Struktur:
- Public API bleibt stabil; interne Verantwortungen sind getrennt (`Detection` SSOT, `Infrastructure` Gate/Backend/Extractor, `Configuration` Policies, `Abstractions` Rückgabemodelle).
- Wiederverwendung erfolgt über ein neutrales Entry-Modell + einheitliche Gate- und Extractor-Pfade.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
