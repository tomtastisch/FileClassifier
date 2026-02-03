# Index - Infrastructure

## 1. Zweck
Dieser Unterordner kapselt technische Infrastruktur und Sicherheitsbausteine fuer die Dateityp-Erkennung:
- MIME-Aufloesung
- Bounded I/O
- ZIP-Sicherheitspruefung
- OOXML-Verfeinerung
- defensives Logging

## 2. Geltungsbereich
Gilt fuer alle nicht-fachlichen Hilfskomponenten, die von `FileTypeDetector` indirekt genutzt werden.

## 3. Dateien und Verantwortung
| Datei | Kerninhalte | Sichtbarkeit |
|---|---|---|
| `MimeProvider.vb` | `MimeProvider`, optionaler Backend-Toggle (`USE_ASPNETCORE_MIME`), `MimeProviderDiagnostics`. | `Friend` |
| `Internals.vb` | `StreamBounds`, `IContentSniffer`, `LibMagicSniffer`, `ZipSafetyGate`, `OpenXmlRefiner`, `LogGuard`. | `Friend` |

## 4. Normorientierte Regeln (einheitliche Spezifikation)
1. **Separation of Concerns:** Fachentscheidung bleibt in `FileTypeDetector`; Infrastruktur liefert nur Teilinformationen.
2. **Fail-Closed:** Bei Fehlern in Sniffer/ZIP/OOXML/Logging wird kein unsicheres Positivergebnis erzeugt.
3. **Bounded Processing:** Lesen und Entpacken erfolgt stets mit Grenzwerten aus `FileTypeDetectorOptions`.
4. **Austauschbarkeit:** MIME-Backend ist ueber Compile-Time-Toggle austauschbar.

## 5. Schnittstellenvertrag
### 5.1 `StreamBounds.CopyBounded`
- Kopiert Stream-Daten bis `maxBytes`.
- Bei Ueberschreitung: `InvalidOperationException`.

### 5.2 `LibMagicSniffer.SniffAlias`
- Liefert normalisierten Alias oder `Nothing`.
- Schreibt Debug-Logs bei Fehlern, wirft aber keine Fehler nach aussen.

### 5.3 `ZipSafetyGate`
- `IsZipSafeBytes` und `IsZipSafeStream` liefern strikt `Boolean`.
- Prueft Limits fuer:
  - Entry-Anzahl
  - unkomprimierte Groessen (gesamt/pro Entry)
  - Kompressionsverhaeltnis
  - verschachtelte ZIPs (Tiefe + nested bytes)

### 5.4 `OpenXmlRefiner.TryRefine`
- Erkennt `Docx`, `Xlsx`, `Pptx` ueber kanonische Paketpfade.
- Bei Fehler oder fehlendem Nachweis: `Unknown`.

### 5.5 `LogGuard`
- Defensives Logging (`Debug`, `Warn`, `Error`).
- Logging darf nie die Erkennung unterbrechen.

## 6. Fehler- und Sicherheitsverhalten
- ZIP- oder Stream-Fehler fuehren zu sicherem `False`/`Unknown`.
- Logging-Fehler werden geschluckt (keine fachliche Seiteneffekte).
- Keine unlimitierte Rekursion in verschachtelten ZIP-Strukturen.

## 7. Aenderungsregeln
- Bei Aenderungen an Sicherheitsgrenzen immer:
  1) Optionen (`FileTypeDetectorOptions`) pruefen  
  2) Verhalten von `ZipSafetyGate` gegen Edge-Cases validieren  
  3) Dokumentation (dieses Dokument + `README.md`) synchron halten.
