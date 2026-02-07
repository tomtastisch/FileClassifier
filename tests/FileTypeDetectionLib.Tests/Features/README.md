# Index - Features

## 1. Zweck

Zentraler Einstieg für alle ausführbaren BDD-Feature-Dateien der Test-Suite.

## 2. Feature Registry

| Feature-Datei                                                      | Fokus                                                                                                       | Wichtige Tags                                                              |
|--------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------|
| `FTD_BDD_010_API_DETEKTION_UND_POLICY.feature`                     | API-Detektion, Endungsprüfung und fail-closed Basispfade                                                    | `@unit`, `@api`, `@detector`, `@positiv`, `@negativ`                       |
| `FTD_BDD_020_ARCHIVE_REFINEMENT_UND_VALIDIERUNG.feature`           | Archiv-Gate, OOXML-Refinement und Typisierung auf `Zip` bzw. `Docx`/`Xlsx`/`Pptx`                             | `@integration`, `@detector`, `@processing`, `@archive`, `@refinement`      |
| `FTD_BDD_030_MATERIALIZER_UND_PAYLOAD_FLOWS.feature`               | Materializer- und Payload-Flows inkl. Negativfälle                                                          | `@e2e`, `@materializer`, `@processing`, `@archive`, `@positiv`, `@negativ` |
| `FTD_BDD_040_ARCHIVE_TYPEN_BYTEARRAY_UND_MATERIALISIERUNG.feature` | Einheitliches Byte-Array-Verhalten für mehrere Archivformate bei Detect/Validate/Extract/Materialize        | `@integration`, `@archive`, `@processing`, `@materializer`, `@positiv`     |
| `FTD_BDD_050_DETERMINISTISCHES_HASHING_UND_ROUNDTRIP.feature`      | Deterministische Hash-Evidence, h1-h4 RoundTrip sowie Entry-Byte->Materialize Invarianz inkl. Negativfällen | `@e2e`, `@hashing`, `@deterministic`, `@roundtrip`, `@positiv`, `@negativ` |

## 3. Konventionen

- Gemeinsame Preconditions für Ressourcen werden tabellarisch im `Hintergrund` gepflegt (
  `die folgenden Ressourcen existieren`).
- Wiederholte Testfälle bevorzugt als `Szenariogrundriss` mit `Beispiele`.
- Dateinamen sind flow-orientiert statt test-level-orientiert; die Einordnung erfolgt über Tags.
- Der Ordner `Features/` enthält nur `.feature`-Dateien; Code-Behind liegt in `obj/`.

## 4. Step-Bindings

- Zugehörige Step-Definitionen: `../Steps/FileTypeDetectionSteps.cs`.

## 5. Verifikation und Nachweise

- Ausführung: `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal`.

## Dokumentpflege-Checkliste

- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
