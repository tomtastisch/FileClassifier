# Guides - Change Playbooks

## 1. Zweck
Diese Ebene unter `docs/guides/` liefert praxisnahe Playbooks fuer wiederkehrende Aenderungen:
- Optionen neu anlegen oder anpassen
- neue Datatypes/Modelle sauber ergaenzen

Ziel ist, dass Entwickler und Verwender ohne Rueckfragen sehen:
1. wo geaendert werden muss,
2. wann die Aenderung aktiv wird,
3. wie die Aenderung verifiziert wird.

## 2. Playbooks
1. [OPTIONS_CHANGE_GUIDE.md](./OPTIONS_CHANGE_GUIDE.md)
2. [DATATYPE_EXTENSION_GUIDE.md](./DATATYPE_EXTENSION_GUIDE.md)

## 3. Wann welcher Guide?
| Frage | Guide |
|---|---|
| Neue oder geaenderte Konfigurationsoption? | `OPTIONS_CHANGE_GUIDE.md` |
| Neuer erkennbarer Dateityp (`FileKind`) oder Magic/Alias-Update? | `DATATYPE_EXTENSION_GUIDE.md` |
| Neues API-Rueckgabemodell unter `Abstractions/*`? | `DATATYPE_EXTENSION_GUIDE.md` |
| Kombination aus Option + Datatype? | beide Guides, zuerst Options-Guide, dann Datatype-Guide |

## 4. Verknuepfungen
- [Doku-Index](../README.md)
- [01 - Funktionen](../01_FUNCTIONS.md)
- [02 - Gesamtarchitektur und Ablauffluesse](../02_ARCHITECTURE_AND_FLOWS.md)
- [03 - Referenzen](../03_REFERENCES.md)

## 5. Pflegehinweis
Die Guides sind verbindliche Arbeitsvorlagen. Bei strukturellen API-Aenderungen muessen die betroffenen Abschnitte zeitgleich mitgepflegt werden.
