# Index - Integration

## Zweck

Nachweise für deterministisches Verhalten über mehrere Containerformate und echte Fixture-Dateien.

## Testabdeckung

| Testdatei                                 | Fokus                                                                                                                                     |
|-------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| `DeterministicHashingIntegrationTests.cs` | formatübergreifende LogicalHash-Stabilität, h1-h4 RoundTrip-Konsistenz sowie Extract->Bytes->Materialize Hash-Invarianz (positiv/negativ) |

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
