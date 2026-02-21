# Utils Modul

## 1. Zweck
Dieses Verzeichnis enthaelt kleine, wiederverwendbare Utility-Helfer fuer Guards, Enum-Werte und defensive Kopien.

## 2. Inhalt
- `GuardUtils.vb`
- `EnumUtils.vb`
- `IterableUtils.vb`

## 3. API und Verhalten
- Utilities sind stateless und deterministisch.
- `GuardUtils` validiert Argumente fail-closed per Exceptions.
- `EnumUtils` liefert typisierte Enum-Werte ohne LINQ-Zwang in Call-Sites.
- `IterableUtils` erstellt defensive Kopien fuer Array-Rueckgaben.

## 4. Verifikation
- Nutzung erfolgt in Core-/Abstraction-Typen; Korrektheit wird durch bestehende Unit- und Contract-Tests abgesichert.

## 5. Diagramm
N/A

## 6. Verweise
- [Modul-Root](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Code-Quality-Policy](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
