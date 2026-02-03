# Index - Infrastructure

## Zweck
Sicherheits- und Infrastrukturkomponenten fuer Erkennung und ZIP-Verarbeitung.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `Internals.vb` | Bounded-I/O, Sniffer-Adapter, ZIP-Gate, OOXML-Refinement, Logging-Guard. |
| `MimeProvider.vb` | Diagnose und Auswahl des MIME-Backends. |

## Sicherheitsbeitrag
1. Harte Byte-Grenzen gegen Ressourcen-DoS
2. ZIP-Validierung und sichere Extraktion ueber SSOT-Logik
3. Fail-closed Fehlerpfade (`Unknown`/`False`)
