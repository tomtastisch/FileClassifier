# Infrastructure Modul

## 1. Zweck
Dieses Verzeichnis kapselt sicherheitskritische interne Ausführungslogik für Archive, Bounds, Guards und Extraktion.

## 2. Inhalt
- `CoreInternals.vb`: container-spezifische Verfeinerungslogik (OpenXML, Legacy-Office).
- `ArchiveInternals.vb`, `ArchiveManagedInternals.vb`, `MimeProvider.vb`.
- Untermodul `Utils/` als zentrale SSOT fuer interne Guards, Pfad-Policies, Logging und wiederverwendbare I/O-Helfer.

## 3. API und Verhalten
- Erzwingt fail-closed bei Traversal, Link-Entries, Größenlimits und ungültigen Archiven.
- Stellt einheitliche Archiv-Backends und sichere Extraktion bereit.

## 4. Verifikation
- Unit-/Property-Tests decken adversariale Archive, Grenzen und Fehlerpfade ab.

## 5. Diagramm
```mermaid
flowchart LR
    A[Archive Payload] --> B[Safety Gate]
    B --> C[Extractor Engine]
    C --> D[Memory or Disk Output]
```

## 6. Verweise
- [Modulübersicht](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Infrastructure.Utils-Submodul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/Utils/README.md)
- [Architektur und Flows](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
- [Policy CI](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/001_POLICY_CI.MD)
