# Index - Abstractions

## 1. Zweck
Immutable Rueckgabemodelle fuer stabile API-Vertraege.

## 2. Dateien und Rollen
| Datei | Rolle | Typische Verwendung |
|---|---|---|
| [FileKind.vb](./FileKind.vb) | kanonische Typ-Enum | Vergleich/Branching in Consumers |
| [FileType.vb](./FileType.vb) | Detektionsergebnis (Kind, Allowed, Metadaten) | Ergebnis von `Detect*` |
| [DetectionDetail.vb](./DetectionDetail.vb) | auditierbares Detailergebnis | Logging, UI, Audit |
| [ZipExtractedEntry.vb](./ZipExtractedEntry.vb) | In-Memory ZIP-Eintrag | sichere Weiterverarbeitung |

## 3. Garantie
- Keine Seiteneffekte oder I/O in Modellen.
- `Unknown` bleibt fail-closed Fallback.

## 4. Siehe auch
- [Modulindex](../README.md)
- [Funktionsreferenz](../../../docs/01_FUNCTIONS.md)
- [Referenzen](../../../docs/03_REFERENCES.md)
