# Index - Detection

## Datei
- `FileTypeRegistry.vb`

## Parameter-/Policy-Tabelle
| Parameter | Bedeutung | Regel |
|---|---|---|
| `Kind` | kanonische Typ-ID | nur definierte Enum-Werte |
| `CanonicalExtension` | Endungs-Metadatum | kein Sicherheitsbeweis |
| `Aliases` | normalisierte Aliasliste | deterministisch normalisiert |
| `Allowed` | boolesche Freigabe | `Unknown=False`, bekannte Typen `True` |
| `HasDirectHeaderDetection` | Header-Signatur vorhanden | fuer Nicht-ZIP-Typen erforderlich |
| `HasStructuredContainerDetection` | Containerstruktur erkannt | `Docx/Xlsx/Pptx` via ZIP+Marker |
| `HasDirectContentDetection` | Header ODER strukturierte Erkennung | Vertrauensbasis fuer Typentscheidung |

## Fail-closed
- Nicht abgedeckte Inhalte => `Unknown`.
