# Index - Detection

## Zweck
Single Source of Truth fuer Typdefinitionen und Aliasauflosung.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `FileTypeRegistry.vb` | Typkatalog, Aliasnormalisierung, `Resolve`, `ResolveByAlias`. |

## Wann welche Funktion?
- `Resolve(kind)`: wenn bereits ein `FileKind` bestimmt wurde.
- `ResolveByAlias(alias)`: wenn Dateiendung/Alias in Typ ueberfuehrt werden soll.
- `NormalizeAlias(raw)`: vor jeder Aliasverarbeitung fuer deterministische Ergebnisse.
