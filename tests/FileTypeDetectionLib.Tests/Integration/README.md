# Index - Integration

## Zweck
Nachweise fuer deterministisches Verhalten ueber mehrere Containerformate und echte Fixture-Dateien.

## Testabdeckung
| Testdatei | Fokus |
|---|---|
| `DeterministicHashingIntegrationTests.cs` | formatuebergreifende LogicalHash-Stabilitaet, h1-h4 RoundTrip-Konsistenz sowie Extract->Bytes->Materialize Hash-Invarianz (positiv/negativ) |

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprueft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprueft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
