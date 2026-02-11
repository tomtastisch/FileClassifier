# Integration - Scope Hinweis

## Zweck

Dieser Ordner enthält aktuell keine eigenen Integration-Testklassen.
Die hashing-bezogenen Integrationsnachweise wurden bewusst in die Unit-Suite konsolidiert, um alle Hashing-Assertions in einer Datei zu halten.

## Aktueller Stand

- Keine `.cs`-Testklassen unter `tests/FileTypeDetectionLib.Tests/Integration/`.
- Hashing-RoundTrip- und Invarianz-Nachweise liegen in der Unit-Suite:
  [HashingEvidenceTests](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs).
- Ergänzende End-to-End-Flows liegen in der BDD-Feature-Suite:
  [FTD_BDD_050_HASHING_UND_ROUNDTRIP.feature](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/Features/FTD_BDD_050_HASHING_UND_ROUNDTRIP.feature).

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
