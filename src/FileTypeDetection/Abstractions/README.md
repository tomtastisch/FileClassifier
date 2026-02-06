# Index - Abstractions

## 1. Zweck

Immutable Rückgabemodelle für stabile API-Verträge.

## 1.1 Ordnerstruktur (kanonisch)

- `Detection/` -> Detektions-Rückgabemodelle (`FileKind`, `FileType`, `DetectionDetail`)
- `Archive/` -> Archiv-Eintragsmodell (`ZipExtractedEntry`, Typname historisch)
- `Hashing/` -> deterministische Hash-Evidence-Modelle (`DeterministicHash*`)

## 1.2 Unterordner-README (verpflichtend)

- [Detection/README.md](./Detection/README.md)
- [Archive/README.md](./Archive/README.md)
- [Hashing/README.md](./Hashing/README.md)

## 2. Dateien und Rollen

| Datei                                                                                        | Rolle                                         | Typische Verwendung               |
|----------------------------------------------------------------------------------------------|-----------------------------------------------|-----------------------------------|
| [Detection/FileKind.vb](./Detection/FileKind.vb)                                             | kanonische Typ-Enum                           | Vergleich/Branching in Consumers  |
| [Detection/FileType.vb](./Detection/FileType.vb)                                             | Detektionsergebnis (Kind, Allowed, Metadaten) | Ergebnis von `Detect*`            |
| [Detection/DetectionDetail.vb](./Detection/DetectionDetail.vb)                               | auditierbares Detailergebnis                  | Logging, UI, Audit                |
| [Archive/ZipExtractedEntry.vb](./Archive/ZipExtractedEntry.vb)                               | In-Memory Archiv-Eintrag (Typname historisch) | sichere Weiterverarbeitung        |
| [Hashing/DeterministicHashSourceType.vb](./Hashing/DeterministicHashSourceType.vb)           | Quelltyp für Hash-Evidence                    | Trace/Audit                       |
| [Hashing/DeterministicHashDigestSet.vb](./Hashing/DeterministicHashDigestSet.vb)             | Physical/Logical/Fast Digests                 | Integritätsnachweis               |
| [Hashing/DeterministicHashEvidence.vb](./Hashing/DeterministicHashEvidence.vb)               | Hash-Nachweis pro API-Schritt                 | Hashing/Forensik                  |
| [Hashing/DeterministicHashRoundTripReport.vb](./Hashing/DeterministicHashRoundTripReport.vb) | h1-h4 Vergleichsreport                        | RoundTrip-Verifikation            |
| [Hashing/DeterministicHashOptions.vb](./Hashing/DeterministicHashOptions.vb)                 | API-Optionen für Hash-Berechnung              | Steürung fast hash/payload copies |

## 3. Garantie

- Keine Seiteneffekte oder I/O in Modellen.
- `Unknown` bleibt fail-closed Fallback.

## 4. Siehe auch

- [Modulindex](../README.md)
- [Funktionsreferenz](../../../docs/01_FUNCTIONS.md)
- [Referenzen](../../../docs/03_REFERENCES.md)

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
