# Index - Abstractions

## Zweck
Fachliche Basistypen fuer stabile Rueckgabewerte.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `FileKind.vb` | Kanonische Typ-ID fuer alle Erkennungsresultate. |
| `FileType.vb` | Unveraenderliches Ergebnisobjekt (`Kind`, `Mime`, `Allowed`, `Aliases`). |

## Verwendung
- Verwende `FileKind` fuer fachliche Entscheidungen im Anwendungsflow.
- Verwende `FileType` fuer Ausgabe, Logging und Weitergabe in Pipelines.
