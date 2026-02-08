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
1. [Playbook: Options anlegen und anpassen](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/001_GUIDE_OPTIONS.MD)
2. [Playbook: Neue Datatypes und API-Modelle erweitern](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/002_GUIDE_DATATYPE.MD)
3. Playbook: Legacy NuGet auf kanonisches Paket migrieren (`docs/guides/004_GUIDE_MIGRATE_LEGACY_NUGET.MD`)

## 4. Wann welcher Guide?
| Frage | Guide |
|---|---|
| Neue oder geänderte Konfigurationsoption? | `docs/guides/001_GUIDE_OPTIONS.MD` |
| Neuer erkennbarer Dateityp (`FileKind`) oder Magic/Alias-Update? | `docs/guides/002_GUIDE_DATATYPE.MD` |
| Neues API-Rückgabemodell unter `Abstractions/*`? | `docs/guides/002_GUIDE_DATATYPE.MD` |
| Kombination aus Option + Datatype? | beide Guides, zuerst Options-Guide, dann Datatype-Guide |

## 5. Verknüpfungen
- [Doku-Index](https://github.com/tomtastisch/FileClassifier/blob/main/docs/001_INDEX_CORE.MD)
- [01 - Funktionen](https://github.com/tomtastisch/FileClassifier/blob/main/docs/010_API_CORE.MD)
- [02 - Gesamtarchitektur und Ablaufflüsse](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
- [03 - Referenzen](https://github.com/tomtastisch/FileClassifier/blob/main/docs/references/001_REFERENCES_CORE.MD)

## 6. Pflegehinweis
Die Guides sind verbindliche Arbeitsvorlagen. Bei strukturellen API-Änderungen müssen die betroffenen Abschnitte zeitgleich mitgepflegt werden.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
