# Guides - Change Playbooks

## 1. Zweck
Diese Ebene unter `docs/guides/` liefert praxisnahe Playbooks fuer wiederkehrende Aenderungen:
- Optionen neu anlegen oder anpassen
- neue Datatypes/Modelle sauber ergaenzen

Ziel ist, dass Entwickler und Verwender ohne Rueckfragen sehen:
1. wo geaendert werden muss,
2. wann die Aenderung aktiv wird,
3. wie die Aenderung verifiziert wird.

## 2. Einheitliches Format (fuer alle Guides)
Beide Guides folgen derselben Struktur:
1. Zweck und Einsatzbereich
2. zentrale Datei-Map
3. Schritt-fuer-Schritt-Checkliste
4. konkretes Beispiel
5. Flowchart + Sequence
6. Verifikation + Done-Kriterien

## 3. Playbooks
1. [OPTIONS_CHANGE_GUIDE.md](./OPTIONS_CHANGE_GUIDE.md)
2. [DATATYPE_EXTENSION_GUIDE.md](./DATATYPE_EXTENSION_GUIDE.md)

## 4. Wann welcher Guide?
| Frage | Guide |
|---|---|
| Neue oder geaenderte Konfigurationsoption? | `OPTIONS_CHANGE_GUIDE.md` |
| Neuer erkennbarer Dateityp (`FileKind`) oder Magic/Alias-Update? | `DATATYPE_EXTENSION_GUIDE.md` |
| Neues API-Rueckgabemodell unter `Abstractions/*`? | `DATATYPE_EXTENSION_GUIDE.md` |
| Kombination aus Option + Datatype? | beide Guides, zuerst Options-Guide, dann Datatype-Guide |

## 5. Verknuepfungen
- [Doku-Index](../README.md)
- [01 - Funktionen](../01_FUNCTIONS.md)
- [02 - Gesamtarchitektur und Ablauffluesse](../02_ARCHITECTURE_AND_FLOWS.md)
- [03 - Referenzen](../03_REFERENCES.md)

## 6. Pflegehinweis
Die Guides sind verbindliche Arbeitsvorlagen. Bei strukturellen API-Aenderungen muessen die betroffenen Abschnitte zeitgleich mitgepflegt werden.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprueft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprueft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
