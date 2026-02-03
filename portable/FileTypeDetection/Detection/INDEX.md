# Index - Detection

## Zweck
SSOT fuer Typdefinitionen und Aliasauflosung.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `FileTypeRegistry.vb` | Typdefinitionen, Aliasnormalisierung, `Resolve`, `ResolveByAlias`. |

## Schluesselfunktionen
- `NormalizeAlias(raw)`
- `Resolve(kind)`
- `ResolveByAlias(aliasKey)`

## SSOT-Regel
- Typ- und Aliasdaten werden ausschliesslich hier gepflegt.
