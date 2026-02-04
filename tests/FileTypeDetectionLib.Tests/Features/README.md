# Index - Features

## 1. Purpose
Zentraler Einstieg fuer alle ausfuehrbaren BDD-Feature-Dateien der Test-Suite.

## 2. Feature Registry
| Feature-Datei | Fokus | Wichtige Tags |
|---|---|---|
| `file_type_detection.feature` | Dateitypdetektion, ZIP-Refinement, Endungs-Policy, Materializer-Flows | `@materializer`, `@negative` |

## 3. Konventionen
- Gemeinsame Preconditions fuer Ressourcen werden tabellarisch im `Hintergrund` gepflegt (`die folgenden Ressourcen existieren`).
- Wiederholte Testfaelle bevorzugt als `Szenariogrundriss` mit `Beispiele`.
- Materializer-spezifische Szenarien sind mit `@materializer` markiert.

## 4. Step-Bindings
- Zugehoerige Step-Definitionen: `../Steps/FileTypeDetectionSteps.cs`.

## 5. Verification & Evidence
- Ausfuehrung: `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal`.
