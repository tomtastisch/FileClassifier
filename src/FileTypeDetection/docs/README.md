# Documentation Index - FileTypeDetection

## 1. Ziel
Diese Doku ist in drei Ebenen getrennt:
1. Funktionen (oeffentliche API, Signaturen, Beispiele)
2. Gesamtarchitektur und Ablauffluesse
3. Referenzen (Abhaengigkeiten, interne Pfade, Rueckgabemodelle, ReasonCodes)

## 2. Dokumente
1. [01_FUNCTIONS.md](./01_FUNCTIONS.md)
2. [02_ARCHITECTURE_AND_FLOWS.md](./02_ARCHITECTURE_AND_FLOWS.md)
3. [03_REFERENCES.md](./03_REFERENCES.md)
4. [PRODUCTION_READINESS_CHECKLIST.md](./PRODUCTION_READINESS_CHECKLIST.md)
5. [DIN_SPECIFICATION_DE.md](./DIN_SPECIFICATION_DE.md)

## 2.1 Vertiefende Implementierungsquellen
- [../Detection/README.md](../Detection/README.md)
- [../Infrastructure/README.md](../Infrastructure/README.md)
- [../Configuration/README.md](../Configuration/README.md)
- [../Abstractions/README.md](../Abstractions/README.md)

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
Wenn eine neue Public-Methode eingefuehrt wird, muss sie in allen drei Ebenen auftauchen:
- Methodenmatrix in `01_FUNCTIONS.md`
- mindestens ein Ablauf in `02_ARCHITECTURE_AND_FLOWS.md`
- Referenzzuordnung in `03_REFERENCES.md`
