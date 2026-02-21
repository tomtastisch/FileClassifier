# Infrastructure.Utils.Guards Modul

## 1. Zweck
Dieses Untermodul kapselt ausschliesslich Guard- und Policy-Utilities mit fail-closed Verhalten.

## 2. Inhalt
- `ArgumentGuard.vb`: deterministische Argument- und Enum-Validierung.
- `IOGuards.vb`: Stream-Lesbarkeit, Rewind und bounded Copy.
- `ArchiveGuards.vb`: Archive-Payload-, Link- und Entry-Path-Guards.
- `PathResolutionGuard.vb`: sichere FullPath-Aufloesung mit kontrollierter Protokollierung.
- `DestinationPathGuard.vb`: Zielpfad-Policy fuer Materialisierung/Extraktion.
- `LogGuard.vb`: defensives Logging ohne Rekursion.
- `ExceptionFilterGuard.vb`: zentrale Catch-Filter-Sets als SSOT fuer redundanzfreie Exception-Guards.

## 3. API und Verhalten
- Alle Klassen sind stateless und deterministisch.
- Fehlerpfade sind fail-closed und liefern klare Rueckgaben.
- Wiederkehrende Exception-Filter werden zentral ueber `ExceptionFilterGuard` gepflegt.

## 4. Verifikation
- Nutzung erfolgt in `FileMaterializer`, `ArchiveInternals`, `CoreInternals` und Hashing-Komponenten.

## 5. Diagramm
```mermaid
flowchart LR
    A["Call Site"] --> B["Utils/Guards"]
    B --> C["Guard Decision"]
    C --> D["Fail-Closed Output"]
```

## 6. Verweise
- [Utils-Root](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/Utils/README.md)
- [Infrastructure-Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md)
- [Code-Quality-Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
