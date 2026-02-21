# Infrastructure.Utils Modul

## 1. Zweck
Dieses Verzeichnis enthaelt die zentrale SSOT-Schicht fuer interne, wiederverwendbare Utility-Helfer.
Der Fokus liegt auf deterministischen Guards, sicherer Pfad-/Archive-Validierung, defensiver I/O-Hilfe und policy-konformem Logging.

## 2. Inhalt
- `GuardUtils.vb`: Argument-Guards fuer Null-, Enum- und Laengenpruefungen.
- `EnumUtils.vb`: deterministische Enum-Wertauflistung mit optionaler Sortierung und Range.
- `IterableUtils.vb`: defensive Array-Kopien fuer sichere Rueckgaben.
- `IoGuards.vb`: zentrale Stream-/Buffer-Helfer (`StreamGuard`, `StreamBounds`, `InternalIoDefaults`).
- `ArchiveGuards.vb`: Archive-spezifische Guards und Entry-Pfadnormalisierung.
- `PathResolutionGuard.vb`: fail-closed FullPath-Aufloesung mit kontrollierter Protokollierung.
- `DestinationPathGuard.vb`: Zielpfad-Policy fuer Materialisierung und Extraktion.
- `LogGuard.vb`: defensiver Logger-Schutz ohne Rekursion/Seiteneffekte.

## 3. API und Verhalten
- Utilities sind stateless und deterministisch.
- Fehlerpfade sind fail-closed und liefern definierte Rueckgaben oder klar typisierte Exceptions.
- Utility-Klassen sind standardmaessig intern (`Friend`) und kapseln wiederholte Sicherheits-/Validierungsmuster.

## 4. Verifikation
- Nutzung erfolgt in Core-/Infrastructure-/Abstraction-Typen.
- Korrektheit und Verhaltenstreue werden ueber Build-, Unit- und Contract-Tests abgesichert.

## 5. Diagramm
```mermaid
flowchart LR
    A["Call Site"] --> B["Infrastructure.Utils (SSOT)"]
    B --> C["Deterministic Guard / IO / Path Decision"]
    C --> D["Fail-Closed Result"]
```

## 6. Verweise
- [Modul-Root](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Infrastructure-Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md)
- [Code-Quality-Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
