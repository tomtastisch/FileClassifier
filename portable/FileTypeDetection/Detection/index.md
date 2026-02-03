# Index - Detection

## 1. Zweck
Dieser Unterordner enthaelt die zentrale Typ-Registry als **Single Source of Truth (SSOT)** fuer:
- Typmetadaten pro `FileKind`
- Alias-Aufloesung (Dateiendungen/Schluessel -> `FileKind`)

## 2. Geltungsbereich
Gilt fuer alle Typentscheidungen in `FileTypeDetector`, insbesondere:
- `Resolve(kind)`
- `ResolveByAlias(aliasKey)`
- Endungspruefung via kanonische Extension/Aliase

## 3. Dateien und Verantwortung
| Datei | Verantwortung | Sichtbarkeit |
|---|---|---|
| `FileTypeRegistry.vb` | Aufbau und Verwaltung von `TypesByKind` und `KindByAlias`; kanonische Typdefinitionen; Fallback auf `Unknown`. | `Friend` |

## 4. Normorientierte Regeln (einheitliche Spezifikation)
1. **SSOT:** Neue Typen werden ausschliesslich in `KnownTypeDefinitions()` registriert.
2. **Determinismus:** Aliasnormalisierung ist case-insensitive und punktunabhaengig.
3. **Fail-Closed:** Bei unaufloesbaren Schluesseln liefert die Registry `Unknown`.
4. **Konsistenz:** `TypesByKind` und `KindByAlias` werden gemeinsam im statischen Konstruktor aufgebaut.

## 5. Schnittstellenvertrag
### 5.1 `NormalizeAlias(raw As String) As String`
- Entfernt fuehrenden Punkt (`.pdf` -> `pdf`).
- Trimmt Leerzeichen und normalisiert auf `ToLowerInvariant()`.

### 5.2 `Resolve(kind As FileKind) As FileType`
- Liefert registrierten Typ.
- Falls nicht vorhanden: immer `FileKind.Unknown`.

### 5.3 `ResolveByAlias(aliasKey As String) As FileType`
- Loest Alias ueber `KindByAlias`.
- Falls unbekannt: `FileKind.Unknown`.

## 6. Fehler- und Sicherheitsverhalten
- Keine externen Dateizugriffe.
- Keine externe Netzwerkabhaengigkeit.
- Bei Null/ungueltigen Eingaben keine Exception-Propagation nach aussen; Fallback `Unknown`.

## 7. Aenderungsregeln
- Bei neuen Typen/Aliases:
  1) `KnownTypeDefinitions()` erweitern  
  2) build + Testlauf durchfuehren  
  3) portable Doku (`README.md` + Unterordner-Indexe) aktualisieren.
