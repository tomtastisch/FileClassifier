# DIN-orientierte Spezifikation (DE) - FileTypeDetection

## 1. Dokumentenlenkung
- Dokument-ID: `FTD-DIN-SPEC-DE`
- Version: `1.1`
- Sprache: Deutsch
- Geltungsbereich: `src/FileTypeDetection`

## 2. Zweck und Anwendungsbereich
Diese Spezifikation beschreibt Anforderungen und Nachweise fuer die oeffentliche API des Moduls nach einem normorientierten Aufbau (Anforderungen, Architektur, Verifikation, Rueckverfolgbarkeit).
Dieses Dokument dient als normativer Nachweis fuer Freigabe-, Audit- und Compliance-Prozesse.
Die technische Detailbeschreibung der oeffentlichen Schnittstellen ist in `01_FUNCTIONS.md`, `02_ARCHITECTURE_AND_FLOWS.md` und `03_REFERENCES.md` dokumentiert.

## 3. Begriffe und Definitionen
- `fail-closed`: Fehlerpfad liefert nur sichere Standardwerte.
- `SSOT`: Single Source of Truth fuer Erkennungsregeln.
- `Archiv-Gate`: sicherheitsorientierte Validierung vor Archiv-Verarbeitung.

## 4. Normative Anforderungen
| ID | Anforderung | Muss-Kriterium | Nachweis |
|---|---|---|---|
| FTD-REQ-001 | Deterministische Typdetektion | gleiche Eingabe => gleicher `FileKind` | `FileTypeRegistryUnitTests.cs` |
| FTD-REQ-002 | Fail-closed bei Fehlern | keine Ausnahme nach aussen, stattdessen `Unknown`/`False`/leer | `ArchiveAdversarialTests.cs`, `DetectionDetailAndArchiveValidationUnitTests.cs` |
| FTD-REQ-003 | Archiv-Sicherheit vor Extraktion | Traversal, Bomben und ueberschrittene Limits werden abgewiesen | `ArchiveExtractionUnitTests.cs`, `ArchiveGatePropertyTests.cs` |
| FTD-REQ-004 | Zentrale Optionsverwaltung | Laden/Lesen globaler Optionen nur ueber `FileTypeOptions` | `FileTypeOptionsFacadeUnitTests.cs` |
| FTD-REQ-005 | Einheitliche Byte-Persistenz | Byte-Persistenz mit `overwrite`/`secureExtract` in `FileMaterializer` | `FileMaterializerUnitTests.cs` |
| FTD-REQ-006 | Archiv-Alias-Normalisierung | `tar/tgz/gz/bz2/xz/7z/rar/zz` werden alias-seitig als logischer Archivtyp `FileKind.Zip` behandelt | `FileTypeRegistryUnitTests.cs`, `ExtensionCheckUnitTests.cs` |
| FTD-REQ-007 | Defensive Zielpfad-Sicherheit | Root-Pfade duerfen nicht materialisiert werden; Groessenlimit muss gelten | `FileMaterializerUnitTests.cs` |
| FTD-REQ-008 | Defensive Archiv-Extraktionsziele | Root-Pfade duerfen nicht als Archiv-Extraktionsziel verwendet werden | `ArchiveExtractionUnitTests.cs` |

## 5. Architektur und Verantwortlichkeiten
| Klasse | Verantwortung | Nicht-Ziel |
|---|---|---|
| `FileTypeDetector` | Dateitypen erkennen und Sicherheitspruefung durchfuehren | keine Persistenz |
| `ArchiveProcessing` | sichere Archiv-Validierung und In-Memory-Extraktion | keine allgemeine Byte-Persistenz und kein Disk-Write |
| `FileMaterializer` | Byte-Persistenz auf Datei/Verzeichnis mit optionalem Archiv-Extract | keine neue Archiv-Policy |
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
- Funktionsreferenz: `docs/01_FUNCTIONS.md`
- Architektur- und Ablaufreferenz: `docs/02_ARCHITECTURE_AND_FLOWS.md`
- Referenzdokument: `docs/03_REFERENCES.md`
- Modulindex: `src/FileTypeDetection/README.md`
- Unit-/Property-Tests: `tests/FileTypeDetectionLib.Tests`

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprueft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprueft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
