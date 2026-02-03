# Index - Abstractions

## Zweck
Definiert die fachlichen Basistypen, die als stabile, oeffentliche Ergebnisse der Bibliothek dienen.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `FileKind.vb` | Kanonischer Typkatalog (`Unknown`, Medienformate, ZIP/OOXML). |
| `FileType.vb` | Unveraenderliche Metadaten (`Kind`, Extension, Mime, Allowed, Aliases). |

## Regeln
1. `FileKind` ist die einzige fachliche Typ-ID.
2. `FileType` bleibt immutable.
3. `Unknown` bleibt verpflichtender fail-closed Rueckgabewert.
4. Keine I/O- oder Netzwerkabhaengigkeit in diesem Ordner.
