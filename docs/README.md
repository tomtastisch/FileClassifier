# Documentation Index - FileTypeDetection

## 1. Ziel
Diese Doku ist in drei Ebenen getrennt:
1. Funktionen (öffentliche API, Signaturen, Beispiele)
2. Gesamtarchitektur und Ablaufflüsse
3. Referenzen (Abhängigkeiten, interne Pfade, Rückgabemodelle, ReasonCodes)

## 2. Dokumente
1. [01_FUNCTIONS.md](./01_FUNCTIONS.md)
2. [02_ARCHITECTURE_AND_FLOWS.md](./02_ARCHITECTURE_AND_FLOWS.md)
3. [03_REFERENCES.md](./03_REFERENCES.md)
4. [04_DETERMINISTIC_HASHING_API_CONTRACT.md](./04_DETERMINISTIC_HASHING_API_CONTRACT.md)
5. [test-matrix-hashing.md](./test-matrix-hashing.md)
6. [PRODUCTION_READINESS_CHECKLIST.md](./PRODUCTION_READINESS_CHECKLIST.md)
7. [DIN_SPECIFICATION_DE.md](./DIN_SPECIFICATION_DE.md)
8. [guides/README.md](./guides/README.md)
9. [versioning/POLICY.md](./versioning/POLICY.md)
10. [versioning/VERSIONS.md](./versioning/VERSIONS.md)
11. [versioning/CHANGELOG.md](./versioning/CHANGELOG.md)

## 2.1 Change Playbooks (neu)
- [Guides Index](./guides/README.md)
- [Playbook: Options anlegen und anpassen](./guides/OPTIONS_CHANGE_GUIDE.md)
- [Playbook: Neue Datatypes und API-Modelle erweitern](./guides/DATATYPE_EXTENSION_GUIDE.md)

## 2.2 Vertiefende Implementierungsquellen
- [../src/FileTypeDetection/Detection/README.md](../src/FileTypeDetection/Detection/README.md)
- [../src/FileTypeDetection/Infrastructure/README.md](../src/FileTypeDetection/Infrastructure/README.md)
- [../src/FileTypeDetection/Configuration/README.md](../src/FileTypeDetection/Configuration/README.md)
- [../src/FileTypeDetection/Abstractions/README.md](../src/FileTypeDetection/Abstractions/README.md)
- [../src/FileTypeDetection/Abstractions/Detection/README.md](../src/FileTypeDetection/Abstractions/Detection/README.md)
- [../src/FileTypeDetection/Abstractions/Archive/README.md](../src/FileTypeDetection/Abstractions/Archive/README.md)
- [../src/FileTypeDetection/Abstractions/Hashing/README.md](../src/FileTypeDetection/Abstractions/Hashing/README.md)
- [../src/FileClassifier.App/README.md](../src/FileClassifier.App/README.md)

## 3. Einheitliches Schema
Jedes Dokument folgt demselben Muster:
1. Zweck und Scope
2. Definitions-/Legendenblock
3. Hauptinhalt (Tabellen/Diagramme)
4. Praktische Beispiele oder Nachweise
5. Grenzen/Nicht-Ziele

## 4. Diagrammkonventionen
- `flowchart`: Entscheidungs- und Datenfluss
- `sequenceDiagram`: Komponenteninteraktion in zeitlicher Reihenfolge
- `NSD (Nassi-Shneiderman, via flowchart)`: strukturierter Kontrollfluss einzelner Methoden
- Labels mit Sonderzeichen werden gequotet (`["..."]`), um Parserfehler zu vermeiden.

## 5. Pflegehinweis
Wenn eine neue Public-Methode eingeführt wird, muss sie in allen drei Ebenen auftauchen:
- Methodenmatrix in `01_FUNCTIONS.md`
- mindestens ein Ablauf in `02_ARCHITECTURE_AND_FLOWS.md`
- Referenzzuordnung in `03_REFERENCES.md`
- Lokale Markdown-Links und Abschnittsanker werden via `python3 tools/check-markdown-links.py` in CI geprüft.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
