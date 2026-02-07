# 02 - Gesamtarchitektur und Ablaufflüsse

## 1. Zweck und Scope
Dieses Dokument beschreibt die öffentliche API (Use-Cases), die interne Kernpipeline sowie die wesentlichen Laufzeitflüsse (Detektion, Archiv-Validierung, Extraktion, Persistenz).
Es dient als Architektur- und Ablaufreferenz auf Dokumentationsebene und ersetzt keine Code-Reviews der Guards.

## 2. Begriffe und Notation
### 2.1 Konventionen
- Knoten = Verantwortungsbereich, Komponente oder Artefakt.
- Pfeil = Datenfluss oder Aufrufpfad (je nach Diagrammtyp).
- fail-closed = bei Unsicherheit oder Verstoss: `Unknown`, `false` oder leere Liste.

### 2.2 Flow-IDs (Legende)
- `F0`: ReadFileSafe Utility
- `F1`: Detect (Path)
- `F2`: Detect (Bytes)
- `F3`: Archive Validate
- `F4`: Archive Extract to Memory
- `F5`: Archive Extract to Disk
- `F6`: Raw Byte Materialize (Persist)
- `F7`: Global Options/Baseline
- `F8`: Extension Policy Check
- `F9`: Deterministic Hashing / h1-h4 RoundTrip

### 2.3 Mermaid-Layout (global)
Hinweis: Diese `init`-Konfiguration reduziert Kreuzungen und erhöht Lesbarkeit.
Sie kann pro Diagramm überschrieben werden, sollte aber konsistent bleiben.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 80, "rankSpacing": 90}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart LR
  A["Mermaid Init: aktiviert (Referenz)"]
```

### 2.4 Detailquellen für tieferes Drill-Down
- Detection-Details: [Detection Modul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Detection/README.md)
- Infrastructure-Details (Guards/Archive internals): [Infrastructure Modul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Infrastructure/README.md)
- Konfigurationsdetails: [Configuration Modul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Configuration/README.md)
- Rückgabemodelle: [Abstractions Modul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Abstractions/README.md)
- Rückgabemodelle Detection: [Abstractions Detection Modul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Abstractions/Detection/README.md)
- Rückgabemodelle Archive: [Abstractions Archive Modul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Abstractions/Archive/README.md)
- Rückgabemodelle Hashing: [Abstractions Hashing Modul](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/src/FileTypeDetection/Abstractions/Hashing/README.md)
- Funktionskatalog mit Beispielen: [01 - Funktionen](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/docs/010_API_CORE.MD)

## 3. Architekturübersicht (Systemkontext)
### 3.1 E2E-Systemkontext (kompakt)
Dieses Diagramm zeigt nur Verantwortungsbereiche und Hauptdatenflüsse:
Input -> Public API -> Core Pipeline -> Outputs.
Detailentscheidungen (Archivfall, Refinement, Persistenzzweig) folgen in Abschnitt 4.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 80, "rankSpacing": 90}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart LR
  subgraph INPUT["Input"]
    direction TB
    INP["File Path"]
    INB["Byte Payload"]
  end

  subgraph API["Public API"]
    direction TB
    A1["FileTypeDetector"]
    A2["ArchiveProcessing"]
    A3["FileMaterializer"]
    A4["FileTypeOptions / Baseline"]
  end

  subgraph CORE["Core Pipeline"]
    direction TB
    C1["ReadHeader"]
    C1B["DetectByMagic"]
    C2["ArchiveSafetyGate"]
    C3["OpenXmlRefiner"]
    C4["ArchiveExtractor"]
  end

  subgraph OUT["Outputs"]
    direction TB
    O1["FileType / DetectionDetail"]
    O2["Bool (Validate/Persist)"]
    O3["Entries (Memory Extract)"]
    O4["Files/Directories"]
  end

  INP --> A1
  INB --> A1
  INP --> A2
  INB --> A2
  INP --> A3
  INB --> A3

  A1 --> C1 --> C1B --> C2 --> C3 --> O1
  A2 --> C2 --> O2
  A2 --> C4 --> O3
  A3 --> C2
  A3 --> C4 --> O4
  A3 --> O2

  A4 -.-> A1
  A4 -.-> A2
  A4 -.-> A3
```

Kurzlesehilfe:
- `FileTypeOptions/Baseline` ist Konfigurationskontext (gestrichelt), kein Datenfluss.
- `ArchiveSafetyGate` ist das zentrale fail-closed-Gate für archivbezogene Pfade.

## 4. Flussdiagramme (entscheidungsrelevante Abläufe)
### 4.1 Ablauf A: Detektion und Archiv-Validierung
Dieses Diagramm zeigt die Kernentscheidung: `Magic == Archiv?` sowie die fail-closed-Kaskade über `ArchiveSafetyGate`.
Oben: Typdetektion (`FileType`/`DetectionDetail`).
Unten: reine Archiv-Validierung (`bool`) über denselben Gate-Knoten.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 70, "rankSpacing": 80}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart TD
  I1["Input: path or bytes"] --> D1["Detect(...) / DetectDetailed(...)"]
  D1 --> H1["ReadHeader"]
  H1 --> M1["DetectByMagic"]
  M1 --> Q1{"Magic == Archiv?"}

  Q1 -->|"No"| T1["Resolve(kind) -> Type Output"]
  Q1 -->|"Yes"| G1["ArchiveSafetyGate"] --> Q2{"Archiv safe?"}

  Q2 -->|"No"| U1["Unknown (fail-closed)"]
  Q2 -->|"Yes"| R1["OpenXmlRefiner"] --> T2["Refined or Generic Archive -> Type Output"]

  I1 --> V1["TryValidateArchive / ArchiveProcessing.TryValidate(...)"]
  V1 --> G1
  Q2 --> V2["Validate Output: Bool"]
```

Kurzlesehilfe:
- `ArchiveSafetyGate` ist SSOT für Archiv-Sicherheit in den gezeigten Pfaden.
- `OpenXmlRefiner` läuft nur im Archiv-OK-Fall.

### 4.2 Ablauf B: Extraktion (Memory) vs. Persistenz (Disk)
Dieses Diagramm zeigt zwei Archiv-Use-Cases:
(1) sichere In-Memory-Extraktion (Entries-Liste)
(2) Persistenz auf Disk (Raw Write oder Archiv-Extract), jeweils mit fail-closed Ergebnissen.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 80, "rankSpacing": 90}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart LR
%% --- INPUTS ---
    subgraph INPUT["Input"]
        direction TB
        IN["path | bytes"]
        OPT["Options / Baseline"]
    end

%% --- USE CASES ---
    subgraph UC["Public Use-Cases"]
        direction TB
        UC2["ExtractArchiveEntries<br/>(ArchiveProcessing)"]
        UC3["PersistBytes<br/>(FileMaterializer)"]
    end

%% --- ARCHIVE CORE (SSOT) ---
    subgraph CORE["Archive Core (SSOT)"]
        direction TB
        G["ArchiveSafetyGate"]
        X["ArchiveExtractor"]
        G --> X
    end

%% --- OUTPUTS ---
    subgraph OUT["Outputs"]
        direction TB
        O3["Entries (Memory Extract)"]
        O4["Files / Directories"]
        O2A["Bool (Validate)"]
        O2B["Bool (Persist)"]
    end

%% wiring: inputs -> use cases
    IN --> UC2
    IN --> UC3

%% options context (no dataflow)
    OPT -.-> UC2
    OPT -.-> UC3

%% use cases -> gate
    UC2 --> G
    UC3 --> G

%% gate outcomes
    G --> O2A
    UC3 --> O2B

%% extractor outcomes
    X --> O3
    X --> O4
```

Kurzlesehilfe:
- Memory-Extraktion und Persistenz teilen sich Gate/Extractor.
- Persistenz liefert immer `Bool` als Rückgabekontrakt.

## 5. Sequenzflüsse (Runtime-Interaktionen)
### 5.1 Detect(path) mit Archivfall
Dieser Sequenzfluss zeigt den Archivfall im Detektor:
Detektion -> Gate -> optionales Refinement -> Rückgabe.
Der fail-closed-Pfad liefert `Unknown`.

```mermaid
sequenceDiagram
  participant Caller as Consumer
  participant API as FileTypeDetector
  participant REG as FileTypeRegistry
  participant GATE as ArchiveSafetyGate
  participant REF as OpenXmlRefiner

  Caller->>API: Detect(path, verifyExtension)
  API->>API: ReadHeader(path)
  API->>REG: DetectByMagic(header)

  alt non-archive
    REG-->>API: FileKind
    API-->>Caller: FileType
  else archive
    API->>GATE: IsArchiveSafeStream(...)
    GATE-->>API: pass/fail

    alt pass
      API->>REF: TryRefineStream(...)
      REF-->>API: refined-kind | unknown
      API-->>Caller: FileType
    else fail
      API-->>Caller: Unknown
    end
  end
```

### 5.2 Validate + Extract (Memory)
Fokus: Byte-Pfad über `ArchiveProcessing`.
Fail-closed endet mit leerer Liste.

```mermaid
sequenceDiagram
  participant Caller as Consumer
  participant ZP as ArchiveProcessing
  participant Collect as ArchiveEntryCollector

  Caller->>ZP: TryExtractToMemory(data)
  ZP->>Collect: TryCollectFromBytes(data, opt, entries)
  Collect-->>ZP: pass/fail + entries

  alt pass
    ZP-->>Caller: entries list
  else fail
    ZP-->>Caller: empty list
  end
```

### 5.3 Materializer: Branching (Persist)
Fokus: Zielpfadprüfung, danach entweder sicherer Archiv-Zweig oder Raw-Write.
Rückgabe ist immer boolesch.

```mermaid
sequenceDiagram
  participant Caller as Consumer
  participant MAT as FileMaterializer
  participant Guard as DestinationPathGuard
  participant Resolver as ArchiveTypeResolver
  participant Gate as ArchiveSafetyGate
  participant Extract as ArchiveExtractor
  participant FS as FileSystem

  Caller->>MAT: Persist(data, destination, overwrite, secureExtract)
  MAT->>Guard: PrepareMaterializationTarget(destination, overwrite)

  alt invalid target
    MAT-->>Caller: false
  else valid target
    alt secureExtract and archive
      MAT->>Resolver: TryDescribeBytes(data)
      Resolver-->>MAT: descriptor/none
      MAT->>Gate: IsArchiveSafeBytes(data, descriptor)
      Gate-->>MAT: pass/fail

      alt pass
        MAT->>Extract: TryExtractArchiveStream(...)
        Extract-->>Caller: true/false
      else fail
        MAT-->>Caller: false
      end
    else raw write
      MAT->>FS: CreateNew + Write bytes
      FS-->>Caller: true/false
    end
  end
```

## 6. NSD-Sichten (strukturierter Kontrollfluss)
### 6.1 NSD: FileMaterializer.Persist(...)
Diese Sicht reduziert verschachtelte Bedingungen auf strukturierten Kontrollfluss.
Jeder negative Prüfpfad endet sofort fail-closed mit `false`.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 65, "rankSpacing": 70}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart TD
  S0["Start Persist(...)"] --> S1{"Input valid?<br/>(data, size, destination)"}
  S1 -->|"No"| E1["Return false"]
  S1 -->|"Yes"| S2{"secureExtract and archive?"}

  S2 -->|"No"| A1["MaterializeRawBytes(...)"] --> R1["Return bool"]
  S2 -->|"Yes"| S3{"Readable archive?"}

  S3 -->|"No"| E2["Return false"]
  S3 -->|"Yes"| S4{"ArchiveSafetyGate pass?"}

  S4 -->|"No"| E3["Return false"]
  S4 -->|"Yes"| A2["MaterializeArchiveBytes(...)"] --> R2["Return bool"]
```

### 6.2 NSD: FileTypeDetector.Detect(path, verifyExtension)
Die Endungsprüfung ist ein nachgelagerter Policy-Filter.
Bei Mismatch wird fail-closed `UnknownType` zurückgegeben.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 65, "rankSpacing": 70}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart TD
  D0["Start Detect(path, verifyExtension)"] --> D1["detected := DetectPathCore(path)"]
  D1 --> D2{"verifyExtension?"}

  D2 -->|"No"| D3["Return detected"]
  D2 -->|"Yes"| D4{"ExtensionMatchesKind(path, detected.Kind)?"}

  D4 -->|"Yes"| D5["Return detected"]
  D4 -->|"No"| D6["Return UnknownType"]
```

## 7. Zuordnung Public API -> Flows
| Methode | Flow-ID |
|---|---|
| `ReadFileSafe(path)` | `F0` |
| `Detect(path)` / `DetectDetailed(path)` | `F1` |
| `Detect(data)` / `IsOfType(data, kind)` | `F2` |
| `TryValidateArchive(path)` / `ArchiveProcessing.TryValidate(path|data)` | `F3` |
| `ExtractArchiveSafeToMemory(path, ...)` / `ArchiveProcessing.ExtractToMemory(...)` / `ArchiveProcessing.TryExtractToMemory(data)` | `F4` |
| `ExtractArchiveSafe(path, destination, ...)` | `F5` |
| `FileMaterializer.Persist(..., secureExtract:=False)` | `F6` |
| `FileTypeOptions.LoadOptions/GetOptions` / `FileTypeProjectBaseline.ApplyDeterministicDefaults` | `F7` |
| `DetectAndVerifyExtension(path)` / `Detect(..., verifyExtension)` | `F8` |
| `DeterministicHashing.HashFile/HashBytes/HashEntries/VerifyRoundTrip` | `F9` |

## 8. Grenzen und Nicht-Ziele
- Kein Ersatz für Quellcode-Reviews interner Guards (z. B. Payload-/Path-Guards).
- Keine Policy-Festlegung für konkrete Grenzwerte; diese kommen aus `FileTypeProjectOptions` und der Baseline.
- Keine Aussage über konkrete Threat-Model-Abdeckung ausserhalb der beschriebenen fail-closed-Semantik.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
