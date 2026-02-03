# Index - Infrastructure

## Zweck
Sicherheits- und Infrastrukturkomponenten fuer Erkennung und ZIP-Verarbeitung.

## Dateien und Verantwortung
| Datei | Verantwortung |
|---|---|
| `Internals.vb` | `StreamBounds`, `LibMagicSniffer`, `ZipSafetyGate`, `OpenXmlRefiner`, `LogGuard`. |
| `MimeProvider.vb` | MIME-Aufloesung und Backend-Diagnostik. |

## Sicherheitsrelevante Punkte
1. ZIP-Sicherheitspruefung + Extraktion teilen dieselbe Kernlogik
2. Path-Traversal-Blockade beim Entpacken
3. Nested-ZIP-Limits und bounded copy
4. Atomare Zielverzeichnis-Erstellung (stage + move)
