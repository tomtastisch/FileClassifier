# 02 - Gesamtarchitektur und Ablauffluesse

## 1. Zweck und Scope
Dieses Dokument beschreibt die oeffentliche API (Use-Cases), die interne Kernpipeline sowie die wesentlichen Laufzeitfluesse (Detektion, ZIP-Validierung, Extraktion, Persistenz).
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
- `F3`: ZIP Validate
- `F4`: ZIP Extract to Memory
- `F5`: ZIP Extract to Disk
- `F6`: Raw Byte Materialize (Persist)
- `F7`: Global Options/Baseline
- `F8`: Extension Policy Check
- `F9`: Deterministic Hashing / h1-h4 RoundTrip

### 2.3 Mermaid-Layout (global)
Hinweis: Diese `init`-Konfiguration reduziert Kreuzungen und erhoeht Lesbarkeit.
Sie kann pro Diagramm ueberschrieben werden, sollte aber konsistent bleiben.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 80, "rankSpacing": 90}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart LR
  A["Mermaid Init: aktiviert (Referenz)"]
```

### 2.4 Detailquellen fuer tieferes Drill-Down
- Detection-Details: [`../src/FileTypeDetection/Detection/README.md`](../src/FileTypeDetection/Detection/README.md)
- Infrastructure-Details (Guards/ZIP internals): [`../src/FileTypeDetection/Infrastructure/README.md`](../src/FileTypeDetection/Infrastructure/README.md)
- Konfigurationsdetails: [`../src/FileTypeDetection/Configuration/README.md`](../src/FileTypeDetection/Configuration/README.md)
- Rueckgabemodelle: [`../src/FileTypeDetection/Abstractions/README.md`](../src/FileTypeDetection/Abstractions/README.md)
- Funktionskatalog mit Beispielen: [`./01_FUNCTIONS.md`](./01_FUNCTIONS.md)

## 3. Architekturuebersicht (Systemkontext)
### 3.1 E2E-Systemkontext (kompakt)
Dieses Diagramm zeigt nur Verantwortungsbereiche und Hauptdatenfluesse:
Input -> Public API -> Core Pipeline -> Outputs.
Detailentscheidungen (ZIP-Fall, Refinement, Persistenzzweig) folgen in Abschnitt 4.

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
    A2["ZipProcessing"]
    A3["FileMaterializer"]
    A4["FileTypeOptions / Baseline"]
  end

  subgraph CORE["Core Pipeline"]
    direction TB
    C1["ReadHeader"]
    C1B["DetectByMagic"]
    C2["ZipSafetyGate"]
    C3["OpenXmlRefiner"]
    C4["ZipExtractor"]
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
- `ZipSafetyGate` ist das zentrale fail-closed-Gate fuer ZIP-bezogene Pfade.

## 4. Flussdiagramme (entscheidungsrelevante Ablaeufe)
### 4.1 Ablauf A: Detektion und ZIP-Validierung
Dieses Diagramm zeigt die Kernentscheidung: `Magic == ZIP?` sowie die fail-closed-Kaskade ueber `ZipSafetyGate`.
Oben: Typdetektion (`FileType`/`DetectionDetail`).
Unten: reine ZIP-Validierung (`bool`) ueber denselben Gate-Knoten.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 70, "rankSpacing": 80}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart TD
  I1["Input: path or bytes"] --> D1["Detect(...) / DetectDetailed(...)"]
  D1 --> H1["ReadHeader"]
  H1 --> M1["DetectByMagic"]
  M1 --> Q1{"Magic == ZIP?"}

  Q1 -->|"No"| T1["Resolve(kind) -> Type Output"]
  Q1 -->|"Yes"| G1["ZipSafetyGate"] --> Q2{"ZIP safe?"}

  Q2 -->|"No"| U1["Unknown (fail-closed)"]
  Q2 -->|"Yes"| R1["OpenXmlRefiner"] --> T2["Refined or Generic ZIP -> Type Output"]

  I1 --> V1["TryValidateZip / ZipProcessing.TryValidate(...)"]
  V1 --> G1
  Q2 --> V2["Validate Output: Bool"]
```

Kurzlesehilfe:
- `ZipSafetyGate` ist SSOT fuer ZIP-Sicherheit in den gezeigten Pfaden.
- `OpenXmlRefiner` laeuft nur im ZIP-OK-Fall.

### 4.2 Ablauf B: Extraktion (Memory) vs. Persistenz (Disk)
Dieses Diagramm zeigt zwei ZIP-Use-Cases:
(1) sichere In-Memory-Extraktion (Entries-Liste)
(2) Persistenz auf Disk (Raw Write oder ZIP-Extract), jeweils mit fail-closed Ergebnissen.

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
        UC2["ExtractZipEntries<br/>(ZipProcessing)"]
        UC3["PersistBytes<br/>(FileMaterializer)"]
    end

%% --- ZIP CORE (SSOT) ---
    subgraph CORE["ZIP Core (SSOT)"]
        direction TB
        G["ZipSafetyGate"]
        X["ZipExtractor"]
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
- Persistenz liefert immer `Bool` als Rueckgabekontrakt.

## 5. Sequenzfluesse (Runtime-Interaktionen)
### 5.1 Detect(path) mit ZIP-Fall
Dieser Sequenzfluss zeigt den ZIP-Fall im Detektor:
Detektion -> Gate -> optionales Refinement -> Rueckgabe.
Der fail-closed-Pfad liefert `Unknown`.

```mermaid
sequenceDiagram
  participant Caller as Consumer
  participant API as FileTypeDetector
  participant REG as FileTypeRegistry
  participant GATE as ZipSafetyGate
  participant REF as OpenXmlRefiner

  Caller->>API: Detect(path, verifyExtension)
  API->>API: ReadHeader(path)
  API->>REG: DetectByMagic(header)

  alt non-zip
    REG-->>API: FileKind
    API-->>Caller: FileType
  else zip
    API->>GATE: IsZipSafeStream(...)
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
Fokus: Byte-Pfad ueber `ZipProcessing`.
Fail-closed endet mit leerer Liste.

```mermaid
sequenceDiagram
  participant Caller as Consumer
  participant ZP as ZipProcessing
  participant Guard as ZipPayloadGuard
  participant Gate as ZipSafetyGate
  participant Extract as ZipExtractor

  Caller->>ZP: TryExtractToMemory(data)
  ZP->>Guard: IsSafeZipPayload(data)
  Guard->>Gate: IsZipSafeBytes(data)
  Gate-->>Guard: pass/fail

  alt pass
    ZP->>Extract: TryExtractZipStreamToMemory(...)
    Extract-->>Caller: entries list
  else fail
    ZP-->>Caller: empty list
  end
```

### 5.3 Materializer: Branching (Persist)
Fokus: Zielpfadpruefung, danach entweder sicherer ZIP-Zweig oder Raw-Write.
Rueckgabe ist immer boolesch.

```mermaid
sequenceDiagram
  participant Caller as Consumer
  participant MAT as FileMaterializer
  participant Guard as DestinationPathGuard
  participant Gate as ZipSafetyGate
  participant Extract as ZipExtractor
  participant FS as FileSystem

  Caller->>MAT: Persist(data, destination, overwrite, secureExtract)
  MAT->>Guard: PrepareMaterializationTarget(destination, overwrite)

  alt invalid target
    MAT-->>Caller: false
  else valid target
    alt secureExtract and zip
      MAT->>Gate: IsZipSafeBytes(data)
      Gate-->>MAT: pass/fail

      alt pass
        MAT->>Extract: TryExtractZipStream(...)
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
Jeder negative Pruefpfad endet sofort fail-closed mit `false`.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 65, "rankSpacing": 70}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart TD
  S0["Start Persist(...)"] --> S1{"Input valid?<br/>(data, size, destination)"}
  S1 -->|"No"| E1["Return false"]
  S1 -->|"Yes"| S2{"secureExtract and ZIP?"}

  S2 -->|"No"| A1["MaterializeRawBytes(...)"] --> R1["Return bool"]
  S2 -->|"Yes"| S3{"Readable ZIP?"}

  S3 -->|"No"| E2["Return false"]
  S3 -->|"Yes"| S4{"ZipSafetyGate pass?"}

  S4 -->|"No"| E3["Return false"]
  S4 -->|"Yes"| A2["MaterializeZipBytes(...)"] --> R2["Return bool"]
```

### 6.2 NSD: FileTypeDetector.Detect(path, verifyExtension)
Die Endungspruefung ist ein nachgelagerter Policy-Filter.
Bei Mismatch wird fail-closed `UnknownType` zurueckgegeben.

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
| `TryValidateZip(path)` / `ZipProcessing.TryValidate(path|data)` | `F3` |
| `ExtractZipSafeToMemory(path, ...)` / `ZipProcessing.ExtractToMemory(...)` / `ZipProcessing.TryExtractToMemory(data)` | `F4` |
| `ExtractZipSafe(path, destination, ...)` | `F5` |
| `FileMaterializer.Persist(..., secureExtract:=False)` | `F6` |
| `FileTypeOptions.LoadOptions/GetOptions` / `FileTypeSecurityBaseline.ApplyDeterministicDefaults` | `F7` |
| `DetectAndVerifyExtension(path)` / `Detect(..., verifyExtension)` | `F8` |
| `DeterministicHashing.HashFile/HashBytes/HashEntries/VerifyRoundTrip` | `F9` |

## 8. Grenzen und Nicht-Ziele
- Kein Ersatz fuer Quellcode-Reviews interner Guards (z. B. Payload-/Path-Guards).
- Keine Policy-Festlegung fuer konkrete Grenzwerte; diese kommen aus `FileTypeDetectorOptions` und der Baseline.
- Keine Aussage ueber konkrete Threat-Model-Abdeckung ausserhalb der beschriebenen fail-closed-Semantik.
