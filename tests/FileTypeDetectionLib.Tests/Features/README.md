# Index - Features

## 1. Purpose
Zentraler Einstieg fuer alle ausfuehrbaren BDD-Feature-Dateien der Test-Suite.

## 2. Feature Registry
| Feature-Datei | Fokus | Wichtige Tags |
|---|---|---|
| `FTD_BDD_010_API_DETEKTION_UND_POLICY.feature` | API-Detektion, Endungspruefung und fail-closed Basispfade | `@unit`, `@api`, `@detector`, `@positiv`, `@negativ` |
| `FTD_BDD_020_ZIP_REFINEMENT_UND_VALIDIERUNG.feature` | Archiv-Gate, OOXML-Refinement und Archiv-Typisierung (logisch `FileKind.Zip`) | `@integration`, `@detector`, `@processing`, `@zip`, `@refinement` |
| `FTD_BDD_030_MATERIALIZER_UND_PAYLOAD_FLOWS.feature` | Materializer- und Payload-Flows inkl. Negativfaelle | `@e2e`, `@materializer`, `@processing`, `@zip`, `@positiv`, `@negativ` |
| `FTD_BDD_040_ARCHIVE_TYPEN_BYTEARRAY_UND_MATERIALISIERUNG.feature` | Einheitliches Byte-Array-Verhalten fuer Archivformate (ZIP/TAR/TAR.GZ/7z/RAR) bei Detect/Validate/Extract/Materialize | `@integration`, `@archive`, `@processing`, `@materializer`, `@zip`, `@positiv` |
| `FTD_BDD_050_DETERMINISTISCHES_HASHING_UND_ROUNDTRIP.feature` | Deterministische Hash-Evidence, h1-h4 RoundTrip sowie Entry-Byte->Materialize Invarianz inkl. Negativfaellen | `@e2e`, `@hashing`, `@deterministic`, `@roundtrip`, `@positiv`, `@negativ` |

## 3. Konventionen
- Gemeinsame Preconditions fuer Ressourcen werden tabellarisch im `Hintergrund` gepflegt (`die folgenden Ressourcen existieren`).
- Wiederholte Testfaelle bevorzugt als `Szenariogrundriss` mit `Beispiele`.
- Dateinamen sind flow-orientiert statt test-level-orientiert; die Einordnung erfolgt ueber Tags.
- Der Ordner `Features/` enthaelt nur `.feature`-Dateien; Code-Behind liegt in `obj/`.

## 4. Step-Bindings
- Zugehoerige Step-Definitionen: `../Steps/FileTypeDetectionSteps.cs`.

## 5. Verification & Evidence
- Ausfuehrung: `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal`.
