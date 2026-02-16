# 02 - Overall Architecture and Execution Flows

## 1. Purpose and Scope
This document describes the public API (use cases), the internal core pipeline, and the main runtime flows (detection, archive validation, extraction, persistence).
It serves as an architecture and flow reference at documentation level and is not a replacement for code reviews of internal guards.

## 2. Terms and Notation
### 2.1 Conventions
- node = responsibility, component, or artifact.
- arrow = dataflow or call path (depending on diagram type).
- fail-closed = on uncertainty or violation: `Unknown`, `false`, or an empty list.

### 2.2 Flow IDs (legend)
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

### 2.3 Mermaid layout (global)
Note: This `init` configuration reduces crossings and improves readability.
It can be overridden per diagram, but should remain consistent.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 80, "rankSpacing": 90}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart LR
  A["Mermaid Init: aktiviert (Referenz)"]
```

### 2.4 Detail sources for deeper drill-down
- Detection details: [Detection module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Detection/README.md)
- Infrastructure details (guards/archive internals): [Infrastructure module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md)
- Configuration details: [Configuration module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Configuration/README.md)
- Return models: [Abstractions module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)
- Return models (detection): [Abstractions Detection module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Detection/README.md)
- Return models (archive): [Abstractions Archive module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Archive/README.md)
- Return models (hashing): [Abstractions Hashing module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Hashing/README.md)
- Function catalog with examples: [01 - Functions](https://github.com/tomtastisch/FileClassifier/blob/main/docs/110_API_CORE.MD)

## 3. Architecture Overview (System Context)
### 3.1 E2E system context (compact)
This diagram shows the layers of the runtime flow:
inputs -> public API -> core components -> outputs.
Branching decisions (archive case, refinement, persistence branch) follow in 3.2 and section 4.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 80, "rankSpacing": 90}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart LR
    subgraph INPUT["Input"]
        FP["File Path"]
        BP["Byte Payload"]
    end

    subgraph API["Public API"]
        FTD["FileTypeDetector"]
        AP["ArchiveProcessing"]
        FM["FileMaterializer"]
        DH["EvidenceHashing"]
    end

    subgraph CORE["Kernkomponenten"]
        REG["FileTypeRegistry"]
        GATE["ArchiveSafetyGate"]
        EXT["ArchiveExtractor"]
        COL["ArchiveEntryCollector"]
        REF["OpenXmlRefiner"]
    end

    subgraph OUT["Outputs"]
        T["FileType / DetectionDetail"]
        V["Bool"]
        E["Entries"]
        F["Files / Directories"]
        H["HashEvidence / RoundTripReport"]
    end

    FP --> FTD
    BP --> FTD
    FP --> AP
    BP --> AP
    BP --> FM
    FP --> DH
    BP --> DH

    FTD --> REG
    FTD --> GATE
    FTD --> REF
    AP --> GATE
    AP --> EXT
    FM --> EXT
    DH --> COL
    DH --> REG

    FTD --> T
    AP --> V
    AP --> E
    FM --> V
    FM --> F
    DH --> H
```

Quick reading guide:
- layer view only, no branch details.
- branching and security decisions are covered in section 4.

### 3.2 Decision overview (compact)
This diagram shows the core decision logic in a single vertical reading path.

```mermaid
%%{init: {"flowchart": {"curve": "linear", "nodeSpacing": 80, "rankSpacing": 90}, "themeVariables": {"fontSize": "16px"}}}%%
flowchart TD
  A["Input lesen"] --> B{"Magic erkannt?"}
  B -- "Nein" --> U["Unknown / fail-closed"]
  B -- "Ja" --> C{"Archivtyp?"}
  C -- "Nein" --> T["Direkter Typ-Output"]
  C -- "Ja" --> G["ArchiveSafetyGate"]
  G -- "Fail" --> U
  G -- "Pass" --> R{"OOXML Marker?"}
  R -- "Ja" --> O["OpenXmlRefiner"]
  R -- "Nein" --> Z["Archive Generic"]
  O --> T
  Z --> T
```

## 4. Flowcharts (decision-relevant flows)
### 4.1 Flow A: Detection and archive validation
This diagram shows the key decision: `magic == archive?` and the fail-closed cascade via `ArchiveSafetyGate`.
Top: type detection (`FileType`/`DetectionDetail`).
Bottom: pure archive validation (`bool`) via the same gate node.

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

Quick reading guide:
- `ArchiveSafetyGate` is the SSOT for archive safety on the shown paths.
- `OpenXmlRefiner` runs only when the archive passes the gate.

### 4.2 Flow B: Extraction (memory) vs. persistence (disk)
This diagram shows two archive use cases:
(1) safe in-memory extraction (entries list)
(2) persistence to disk (raw write or archive extract), each with fail-closed results.

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

Quick reading guide:
- memory extraction and persistence share the gate/extractor.
- persistence always returns `Bool` as the contract.

## 5. Sequence flows (runtime interactions)
### 5.1 Detect(path) with archive case
This sequence flow shows the archive case in the detector:
detection -> gate -> optional refinement -> return value.
The fail-closed path returns `Unknown`.

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

### 5.2 Validate + extract (memory)
Focus: byte path via `ArchiveProcessing`.
Fail-closed ends with an empty list.

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

### 5.3 Materializer: branching (persist)
Focus: destination-path validation, then either the secure archive branch or raw write.
The return type is always boolean.

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

## 6. NSD views (structured control flow)
### 6.1 NSD: FileMaterializer.Persist(...)
This view reduces nested conditions to structured control flow.
Every negative check path ends immediately fail-closed with `false`.

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
The extension check is a downstream policy filter.
On mismatch, `UnknownType` is returned fail-closed.

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

## 7. Public API to flow mapping
| Method | Flow ID |
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
| `EvidenceHashing.HashFile/HashBytes/HashEntries/VerifyRoundTrip` | `F9` |

## 8. Boundaries and non-goals
- not a replacement for source-code reviews of internal guards (e.g., payload/path guards).
- no policy decision for specific thresholds; these come from `FileTypeProjectOptions` and the baseline.
- no claim about threat-model coverage beyond the described fail-closed semantics.

## Documentation maintenance checklist
- [ ] Content reviewed against current code state.
- [ ] Links and anchors checked via `python3 tools/check-docs.py`.
- [ ] Examples/commands verified locally.
- [ ] Terminology aligned with `docs/110_API_CORE.MD`.
