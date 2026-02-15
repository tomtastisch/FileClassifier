# DIN-orientierte Spezifikation (DE) - FileTypeDetection

## 1. Dokumentenlenkung
- Dokument-ID: `FTD-DIN-SPEC-DE`
- Version: `1.1`
- Sprache: Deutsch
- Geltungsbereich: `src/FileTypeDetection`

## 2. Zweck und Anwendungsbereich
Diese Spezifikation beschreibt Anforderungen und Nachweise für die öffentliche API des Moduls nach einem normorientierten Aufbau (Anforderungen, Architektur, Verifikation, Rückverfolgbarkeit).
Dieses Dokument dient als normativer Nachweis für Freigabe-, Audit- und Compliance-Prozesse.
Die technische Detailbeschreibung der öffentlichen Schnittstellen ist in `docs/010_API_CORE.MD`, `docs/020_ARCH_CORE.MD` und `docs/references/001_REFERENCES_CORE.MD` dokumentiert.

## 3. Begriffe und Definitionen
- `fail-closed`: Fehlerpfad liefert nur sichere Standardwerte.
- `SSOT`: Single Source of Truth für Erkennungsregeln.
- `Archiv-Gate`: sicherheitsorientierte Validierung vor Archiv-Verarbeitung.

## 4. Normative Anforderungen
| ID | Anforderung | Muss-Kriterium | Nachweis |
|---|---|---|---|
| FTD-REQ-001 | Deterministische Typdetektion | gleiche Eingabe => gleicher `FileKind` | `FileTypeRegistryUnitTests.cs` |
| FTD-REQ-002 | Fail-closed bei Fehlern | keine Ausnahme nach aussen, stattdessen `Unknown`/`False`/leer | `ArchiveAdversarialTests.cs`, `DetectionDetailAndArchiveValidationUnitTests.cs` |
| FTD-REQ-003 | Archiv-Sicherheit vor Extraktion | Traversal, Bomben und überschrittene Limits werden abgewiesen | `ArchiveExtractionUnitTests.cs`, `ArchiveGatePropertyTests.cs` |
| FTD-REQ-004 | Zentrale Optionsverwaltung | Laden/Lesen globaler Optionen nur über `FileTypeOptions` | `FileTypeOptionsFacadeUnitTests.cs` |
| FTD-REQ-005 | Einheitliche Byte-Persistenz | Byte-Persistenz mit `overwrite`/`secureExtract` in `FileMaterializer` | `FileMaterializerUnitTests.cs` |
| FTD-REQ-006 | Archiv-Alias-Normalisierung | `tar/tgz/gz/bz2/xz/7z/rar/zz` werden alias-seitig als logischer Archivtyp `FileKind.Zip` behandelt | `FileTypeRegistryUnitTests.cs`, `ExtensionCheckUnitTests.cs` |
| FTD-REQ-007 | Defensive Zielpfad-Sicherheit | Root-Pfade dürfen nicht materialisiert werden; Grössenlimit muss gelten | `FileMaterializerUnitTests.cs` |
| FTD-REQ-008 | Defensive Archiv-Extraktionsziele | Root-Pfade dürfen nicht als Archiv-Extraktionsziel verwendet werden | `ArchiveExtractionUnitTests.cs` |

## 5. Architektur und Verantwortlichkeiten
| Klasse | Verantwortung | Nicht-Ziel |
|---|---|---|
| `FileTypeDetector` | Dateitypen erkennen und Sicherheitsprüfung durchführen | keine Persistenz |
| `ArchiveProcessing` | sichere Archiv-Validierung und In-Memory-Extraktion | keine allgemeine Byte-Persistenz und kein Disk-Write |
| `FileMaterializer` | Byte-Persistenz auf Datei/Verzeichnis mit optionalem Archiv-Extract | keine neue Archiv-Policy |
| `FileTypeOptions` | zentrale globale Optionsschnittstelle (`LoadOptions`, `GetOptions`) | keine Typdetektion |

## 6. Schnittstellen und Parameter
- `FileMaterializer.Persist(data, destinationPath, overwrite, secureExtract)`
  - `overwrite` default: `False`
  - `secureExtract` default: `False`
- `FileTypeOptions.LoadOptions(json)`
  - partielle JSON-Werte überschreiben Defaults
- `FileTypeOptions.GetOptions()`
  - Rückgabe als JSON-Snapshot

## 7. Verifikation
Pflichtlauf für Freigabe:
```bash
python3 tools/check-docs.py
dotnet restore FileClassifier.sln -v minimal
dotnet build FileClassifier.sln --no-restore -v minimal
TEST_BDD_OUTPUT_DIR=artifacts/tests bash tools/test-bdd-readable.sh -- \
  /p:CollectCoverage=true \
  /p:Include="[FileTypeDetectionLib]*" \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput="$(pwd)/artifacts/coverage/coverage" \
  /p:Threshold=85%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
```

## 8. Rückverfolgbarkeit
Diese Spezifikation ist rückverfolgbar auf:
- Funktionsreferenz: `docs/010_API_CORE.MD`
- Architektur- und Ablaufreferenz: `docs/020_ARCH_CORE.MD`
- Referenzdokument: `docs/references/001_REFERENCES_CORE.MD`
- Modulindex: `src/FileTypeDetection/README.md`
- Unit-/Property-Tests: `tests/FileTypeDetectionLib.Tests`

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
