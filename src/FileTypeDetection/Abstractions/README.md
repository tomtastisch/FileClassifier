# Index - Abstractions

## 1. Zweck
Immutable Rueckgabemodelle fuer stabile API-Vertraege.

## 1.1 Ordnerstruktur (kanonisch)
- `Detection/` -> Detektions-Rueckgabemodelle (`FileKind`, `FileType`, `DetectionDetail`)
- `Archive/` -> Archiv-Eintragsmodell (`ZipExtractedEntry`, Typname historisch)
- `Hashing/` -> deterministische Hash-Evidence-Modelle (`DeterministicHash*`)

## 1.2 Unterordner-README (verpflichtend)
- [Detection/README.md](./Detection/README.md)
- [Archive/README.md](./Archive/README.md)
- [Hashing/README.md](./Hashing/README.md)

## 2. Dateien und Rollen
| Datei | Rolle | Typische Verwendung |
|---|---|---|
| [Detection/FileKind.vb](./Detection/FileKind.vb) | kanonische Typ-Enum | Vergleich/Branching in Consumers |
| [Detection/FileType.vb](./Detection/FileType.vb) | Detektionsergebnis (Kind, Allowed, Metadaten) | Ergebnis von `Detect*` |
| [Detection/DetectionDetail.vb](./Detection/DetectionDetail.vb) | auditierbares Detailergebnis | Logging, UI, Audit |
| [Archive/ZipExtractedEntry.vb](./Archive/ZipExtractedEntry.vb) | In-Memory Archiv-Eintrag (Typname historisch) | sichere Weiterverarbeitung |
| [Hashing/DeterministicHashSourceType.vb](./Hashing/DeterministicHashSourceType.vb) | Quelltyp fuer Hash-Evidence | Trace/Audit |
| [Hashing/DeterministicHashDigestSet.vb](./Hashing/DeterministicHashDigestSet.vb) | Physical/Logical/Fast Digests | Integritaetsnachweis |
| [Hashing/DeterministicHashEvidence.vb](./Hashing/DeterministicHashEvidence.vb) | Hash-Nachweis pro API-Schritt | Hashing/Forensik |
| [Hashing/DeterministicHashRoundTripReport.vb](./Hashing/DeterministicHashRoundTripReport.vb) | h1-h4 Vergleichsreport | RoundTrip-Verifikation |
| [Hashing/DeterministicHashOptions.vb](./Hashing/DeterministicHashOptions.vb) | API-Optionen fuer Hash-Berechnung | Steuerung fast hash/payload copies |

## 3. Garantie
- Keine Seiteneffekte oder I/O in Modellen.
- `Unknown` bleibt fail-closed Fallback.

## 4. Siehe auch
- [Modulindex](../README.md)
- [Funktionsreferenz](../../../docs/01_FUNCTIONS.md)
- [Referenzen](../../../docs/03_REFERENCES.md)
