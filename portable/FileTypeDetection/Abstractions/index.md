# Index - Abstractions

## 1. Zweck
Dieser Unterordner definiert die fachlichen Grundtypen der Dateityp-Erkennung:
- `FileKind` als kanonischer Typkatalog.
- `FileType` als unveraenderliches Metadatenobjekt.

## 2. Geltungsbereich
Gilt fuer alle Aufrufe von `FileTypeDetector`, `FileTypeRegistry` und deren Rueckgabewerte.

## 3. Dateien und Verantwortung
| Datei | Verantwortung | Sichtbarkeit |
|---|---|---|
| `FileKind.vb` | Definiert alle unterstuetzten Dateitypen als Enum (`Unknown`, `Pdf`, `Png`, `Jpeg`, `Gif`, `Webp`, `Zip`, `Docx`, `Xlsx`, `Pptx`). | `Public` |
| `FileType.vb` | Kapselt Ergebnis-Metadaten (`Kind`, `CanonicalExtension`, `Mime`, `Allowed`, `Aliases`) unveraenderlich. | `Public` |

## 4. Normorientierte Regeln (einheitliche Spezifikation)
1. **Eindeutigkeit:** `FileKind` ist die einzige fachliche Typ-ID.
2. **Unveraenderlichkeit:** `FileType` besitzt nur ReadOnly-Eigenschaften.
3. **Determinismus:** Aliasliste wird im Konstruktor normalisiert und dedupliziert.
4. **Fail-Closed-Kompatibilitaet:** `Unknown` ist obligatorischer Typ.

## 5. Schnittstellenvertrag
### 5.1 `FileKind`
- Keine Logik, nur stabile Enum-Werte.
- Neue Werte nur kontrolliert in Verbindung mit `FileTypeRegistry`.

### 5.2 `FileType`
- Konstruktor ist `Friend` (Instanziierung nur innerhalb der Bibliothek).
- `ToString()` liefert den Enum-Namen (`Kind.ToString()`).

## 6. Fehler- und Sicherheitsverhalten
- Keine externen I/O-Operationen.
- Keine Exceptions bei leerer Aliasliste; stattdessen `ImmutableArray.Empty`.
- Aliasnormalisierung erfolgt zentral ueber `FileTypeRegistry.NormalizeAlias`.

## 7. Aenderungsregeln
- Bei neuem `FileKind` muessen mindestens angepasst werden:
  1) `FileKind.vb`  
  2) `Detection/FileTypeRegistry.vb` (Typdefinitionen/Aliase)  
  3) Dokumentation in `README.md`.
