# Guides - Change Playbooks

## 1. Zweck
Diese Ebene unter `docs/guides/` liefert praxisnahe Playbooks für wiederkehrende Änderungen:
- Optionen neu anlegen oder anpassen
- neue Datatypes/Modelle sauber ergänzen

Ziel ist, dass Entwickler und Verwender ohne Rückfragen sehen:
1. wo geändert werden muss,
2. wann die Änderung aktiv wird,
3. wie die Änderung verifiziert wird.

## 2. Einheitliches Format (für alle Guides)
Beide Guides folgen derselben Struktur:
1. Zweck und Einsatzbereich
2. zentrale Datei-Map
3. Schritt-für-Schritt-Checkliste
4. konkretes Beispiel
5. Flowchart + Sequence
6. Verifikation + Done-Kriterien

## 3. Playbooks
1. [OPTIONS_CHANGE_GUIDE.md](./OPTIONS_CHANGE_GUIDE.md)
2. [DATATYPE_EXTENSION_GUIDE.md](./DATATYPE_EXTENSION_GUIDE.md)

## 4. Wann welcher Guide?
| Frage | Guide |
|---|---|
| Neue oder geänderte Konfigurationsoption? | `OPTIONS_CHANGE_GUIDE.md` |
| Neuer erkennbarer Dateityp (`FileKind`) oder Magic/Alias-Update? | `DATATYPE_EXTENSION_GUIDE.md` |
| Neues API-Rückgabemodell unter `Abstractions/*`? | `DATATYPE_EXTENSION_GUIDE.md` |
| Kombination aus Option + Datatype? | beide Guides, zuerst Options-Guide, dann Datatype-Guide |

## 5. Verknüpfungen
- [Doku-Index](../README.md)
- [01 - Funktionen](../01_FUNCTIONS.md)
- [02 - Gesamtarchitektur und Ablaufflüsse](../02_ARCHITECTURE_AND_FLOWS.md)
- [03 - Referenzen](../03_REFERENCES.md)

## 6. Pflegehinweis
Die Guides sind verbindliche Arbeitsvorlagen. Bei strukturellen API-Änderungen müssen die betroffenen Abschnitte zeitgleich mitgepflegt werden.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
