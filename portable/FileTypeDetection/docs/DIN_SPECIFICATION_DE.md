# DIN-orientierte Spezifikation (DE) - FileTypeDetection

## 1. Dokumentenlenkung
- Dokument-ID: `FTD-DIN-SPEC-DE`
- Version: `1.0`
- Sprache: Deutsch
- Geltungsbereich: `src/FileTypeDetection`

## 2. Zweck und Anwendungsbereich
Diese Spezifikation beschreibt Anforderungen und Nachweise fuer die oeffentliche API des Moduls nach einem normorientierten Aufbau (Anforderungen, Architektur, Verifikation, Rueckverfolgbarkeit).

## 3. Begriffe und Definitionen
- `fail-closed`: Fehlerpfad liefert nur sichere Standardwerte.
- `SSOT`: Single Source of Truth fuer Erkennungsregeln.
- `ZIP-Gate`: sicherheitsorientierte Validierung vor ZIP-Verarbeitung.

## 4. Normative Anforderungen
| ID | Anforderung | Muss-Kriterium | Nachweis |
|---|---|---|---|
| FTD-REQ-001 | Deterministische Typdetektion | gleiche Eingabe => gleicher `FileKind` | `FileTypeRegistryUnitTests.cs` |
| FTD-REQ-002 | Fail-closed bei Fehlern | keine Ausnahme nach aussen, stattdessen `Unknown`/`False`/leer | `ZipAdversarialTests.cs`, `DetectionDetailAndZipValidationUnitTests.cs` |
| FTD-REQ-003 | ZIP-Sicherheit vor Extraktion | Traversal, Bomben und ueberschrittene Limits werden abgewiesen | `ZipExtractionUnitTests.cs`, `ZipGatePropertyTests.cs` |
| FTD-REQ-004 | Zentrale Optionsverwaltung | Laden/Lesen globaler Optionen nur ueber `FileTypeOptions` | `FileTypeOptionsFacadeUnitTests.cs` |
| FTD-REQ-005 | Einheitliche Byte-Persistenz | Byte-Persistenz mit `overwrite`/`secureExtract` in `FileMaterializer` | `FileMaterializerUnitTests.cs` |
| FTD-REQ-006 | Archiv-Alias-Normalisierung | `tar/tgz/gz/bz2/xz/7z/rar/zz` werden alias-seitig als `Zip` behandelt | `FileTypeRegistryUnitTests.cs`, `ExtensionCheckUnitTests.cs` |

## 5. Architektur und Verantwortlichkeiten
| Klasse | Verantwortung | Nicht-Ziel |
|---|---|---|
| `FileTypeDetector` | Dateitypen erkennen und Sicherheitspruefung durchfuehren | keine Persistenz |
| `ZipProcessing` | sichere ZIP-Validierung und ZIP-Extraktion | keine allgemeine Byte-Persistenz |
| `FileMaterializer` | Byte-Persistenz auf Datei/Verzeichnis mit optionalem ZIP-Extract | keine neue ZIP-Policy |
| `FileTypeOptions` | zentrale globale Optionsschnittstelle (`LoadOptions`, `GetOptions`) | keine Typdetektion |

## 6. Schnittstellen und Parameter
- `FileMaterializer.Persist(data, destinationPath, overwrite, secureExtract)`
  - `overwrite` default: `False`
  - `secureExtract` default: `False`
- `FileTypeOptions.LoadOptions(json)`
  - partielle JSON-Werte ueberschreiben Defaults
- `FileTypeOptions.GetOptions()`
  - Rueckgabe als JSON-Snapshot

## 7. Verifikation
Pflichtlauf fuer Freigabe:
```bash
dotnet restore FileClassifier.sln -v minimal
dotnet build FileClassifier.sln --no-restore -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --no-build -v minimal
bash tools/sync-portable-filetypedetection.sh
bash tools/sync-doc-conventions.sh
```

## 8. Rueckverfolgbarkeit
Diese Spezifikation ist rueckverfolgbar auf:
- API-Referenz: `src/FileTypeDetection/docs/API_REFERENCE.md`
- Modulindex: `src/FileTypeDetection/INDEX.md`
- Unit-/Property-Tests: `tests/FileTypeDetectionLib.Tests`
