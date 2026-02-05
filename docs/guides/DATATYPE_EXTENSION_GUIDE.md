# Playbook: Neue Datatypes und API-Modelle erweitern

## 1. Zweck und Zielgruppe
Dieses Playbook beschreibt einen einheitlichen, verbindlichen Ablauf fuer:
1. neue erkennbare Formate ueber `FileKind` + `FileTypeRegistry`
2. neue API-Modelle unter `Abstractions/*`

Zielgruppe:
- Entwickler, die Detection/Registry/Abstractions erweitern
- Reviewer, die Konsistenz und fail-closed Verhalten absichern
- Verwender, die Auswirkungen auf API-Outputs verstehen muessen

## 2. Wann dieses Playbook verwenden?
| Vorhaben | Relevanter Abschnitt |
|---|---|
| Neuer Dateityp (z. B. neue Magic-Signatur) | Abschnitt 3 und 4 |
| Neues Rueckgabemodell fuer Public API | Abschnitt 5 und 6 |
| Neues Format plus neues Modell | Abschnitte 3 bis 6 |

## 3. Neue `FileKind`-Formate: zentrale Stellen
### 3.1 Pflichtdateien
| Bereich | Datei | Aufgabe |
|---|---|---|
| Oeffentliche Typliste | [`../../src/FileTypeDetection/Abstractions/Detection/FileKind.vb`](../../src/FileTypeDetection/Abstractions/Detection/FileKind.vb) | Enum-Wert ergaenzen |
| SSOT Metadaten und Magic | [`../../src/FileTypeDetection/Detection/FileTypeRegistry.vb`](../../src/FileTypeDetection/Detection/FileTypeRegistry.vb) | Extension/Alias/Magic und Resolve-Verhalten |
| Detection-Details | [`../../src/FileTypeDetection/Detection/README.md`](../../src/FileTypeDetection/Detection/README.md) | fachliche Erklaerung nachziehen |
| Registry-Tests | [`../../tests/FileTypeDetectionLib.Tests/Unit/FileTypeRegistryUnitTests.cs`](../../tests/FileTypeDetectionLib.Tests/Unit/FileTypeRegistryUnitTests.cs) | Resolve/Alias/Magic validieren |

### 3.2 Registry-Punkte, die geprueft werden muessen
- `ExtensionOverrides` (nur falls CanonicalExtension vom Enum-Namen abweicht)
- `AliasOverrides` (Legacy-/Kompatibilitaets-Aliase)
- `BuildMagicPatternCatalog()` (Magic-Pattern Aufnahme)
- `HasStructuredContainerDetection()` (falls neuer Typ strukturierte Verfeinerung braucht)
- indirekte Auswirkungen auf `KindsWithoutDirectContentDetection()`

### 3.3 Bei neuen Containerarten zusaetzlich
- [`../../src/FileTypeDetection/Infrastructure/README.md`](../../src/FileTypeDetection/Infrastructure/README.md) pruefen, ob Gate/Extractor den Container bereits traegt.
- Falls nein: Infrastrukturpfad erweitern und Security-Gates (Depth, Size, Traversal, Links) mitziehen.

## 4. Schritt-fuer-Schritt-Checkliste fuer neue `FileKind`
- [ ] Schritt 1: Enum-Wert in `FileKind.vb` anlegen.
- [ ] Schritt 2: `FileTypeRegistry.vb` (Extension/Alias/Magic) aktualisieren.
- [ ] Schritt 3: Bei Bedarf `HasStructuredContainerDetection()` erweitern.
- [ ] Schritt 4: Tests fuer Resolve/Alias/Magic/fail-closed ergaenzen.
- [ ] Schritt 5: Doku in `docs/01_FUNCTIONS.md`, `docs/02_ARCHITECTURE_AND_FLOWS.md`, `docs/03_REFERENCES.md` aktualisieren.
- [ ] Schritt 6: Verifikation laufen lassen (Abschnitt 9).

## 5. Neue API-Modelle (`Abstractions/*`): zentrale Stellen
### 5.1 Pflichtdateien
| Bereich | Datei | Aufgabe |
|---|---|---|
| Modell-Uebersicht | [`../../src/FileTypeDetection/Abstractions/README.md`](../../src/FileTypeDetection/Abstractions/README.md) | passenden Modellbereich bestimmen (`Detection`, `Archive`, `Hashing`) |
| Modellklasse/enum | `src/FileTypeDetection/Abstractions/<Bereich>/<Typ>.vb` | neuen Typ definieren |
| Teilbereich README | `src/FileTypeDetection/Abstractions/<Bereich>/README.md` | Typ in Index aufnehmen |
| API-Katalog | [`../01_FUNCTIONS.md`](../01_FUNCTIONS.md) | neue/veraenderte Rueckgabemodelle eintragen |
| Referenzen | [`../03_REFERENCES.md`](../03_REFERENCES.md) | Modell- und Pfadreferenzen aktualisieren |
| Contract-Doku (falls relevant) | `docs/04_*.md` | nur bei Contract-relevanten Signatur-/Vertragsaenderungen |

### 5.2 Modellregeln
- Modelle bleiben immutable orientiert und ohne I/O-Logik.
- `Unknown`/fail-closed Semantik darf nicht aufgeweicht werden.
- Bestehende Consumer-Vertraege bleiben stabil oder werden als Breaking Change ausgewiesen.

## 6. Schritt-fuer-Schritt-Checkliste fuer neue API-Modelle
- [ ] Schritt 1: Modell im passenden Abstractions-Unterordner anlegen.
- [ ] Schritt 2: Teilbereich-README und `Abstractions/README.md` aktualisieren.
- [ ] Schritt 3: Verwendungen in Public API eintragen (`docs/01_FUNCTIONS.md`, `docs/03_REFERENCES.md`).
- [ ] Schritt 4: Falls API-Contract betroffen, `docs/04_*.md` aktualisieren.
- [ ] Schritt 5: Unit-/Integration-Tests fuer neues Modellverhalten ergaenzen.
- [ ] Schritt 6: Verifikation laufen lassen (Abschnitt 9).

## 7. Beispiele (konkret)
### 7.1 Beispiel A: Neuer `FileKind` `Tiff`
1. `FileKind.vb`: Enum-Wert `Tiff` hinzufuegen.
2. `FileTypeRegistry.vb`: TIFF-Magic und Alias `tif`/`tiff` aufnehmen.
3. `FileTypeRegistryUnitTests.cs`: neue Assertions fuer `ResolveByAlias("tif")` und Magic-Match.
4. Doku in `docs/01_FUNCTIONS.md` und `docs/03_REFERENCES.md` ergaenzen.

### 7.2 Beispiel B: Neues Modell `ArchiveScanEvidence`
1. Datei `src/FileTypeDetection/Abstractions/Hashing/ArchiveScanEvidence.vb` anlegen.
2. `src/FileTypeDetection/Abstractions/Hashing/README.md` und `src/FileTypeDetection/Abstractions/README.md` ergaenzen.
3. In `docs/01_FUNCTIONS.md` und `docs/03_REFERENCES.md` Modell ergaenzen.
4. Wenn oeffentliche Signaturen betroffen sind: passendes `docs/04_*.md` aktualisieren.

## 8. Aktivierung und Einlesen
### 8.1 Flowchart (Code -> Runtime)
```mermaid
flowchart TD
    A["Codeaenderung: FileKind and/or Abstractions"] --> B["Build/Test"]
    B --> C["App/Tests starten neuen Prozess"]
    C --> D["FileTypeRegistry Shared Sub New()"]
    D --> E["BuildDefinitionsFromEnum()"]
    E --> F["BuildAliasMap + BuildMagicRules"]
    F --> G["Detect* Pipeline (ReadHeader -> DetectByMagic -> Resolve)"]
    G --> H["Public Output: FileType/DetectionDetail oder neues Modell"]
```

### 8.2 Sequence (Detect-Pipeline mit Registry)
```mermaid
sequenceDiagram
    participant Consumer
    participant Detector as FileTypeDetector
    participant Registry as FileTypeRegistry
    participant Core as CoreInternals/OpenXmlRefiner

    Consumer->>Detector: Detect(path or bytes)
    Detector->>Registry: DetectByMagic(header)
    Registry-->>Detector: FileKind
    alt archive-like path
        Detector->>Core: Archive gate/refinement
        Core-->>Detector: refined kind or unknown
    end
    Detector->>Registry: Resolve(kind)
    Registry-->>Detector: FileType
    Detector-->>Consumer: FileType or DetectionDetail
```

## 9. Verifikation (Kommandos)
```bash
python3 tools/check-markdown-links.py
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "FullyQualifiedName~FileTypeRegistryUnitTests|FullyQualifiedName~HeaderCoveragePolicyUnitTests" -v minimal
```

Bei Modell- oder Contract-Aenderungen zusaetzlich:
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "FullyQualifiedName~DeterministicHashingApiContractUnitTests|FullyQualifiedName~DeterministicHashingIntegrationTests" -v minimal
```

## 10. Implementierungs-Checklist und Done-Kriterien
### 10.1 Implementierungs-Checklist
- [ ] Enum/Registry/Patterns sind konsistent erweitert.
- [ ] Alias-Normalisierung ist eindeutig (keine Konflikte).
- [ ] fail-closed Verhalten bleibt bei ungueltigen Eingaben erhalten.
- [ ] Tests decken Resolve, Alias, Magic und Regressionen ab.
- [ ] Doku-Ebenen `01/02/03` und Modul-READMEs sind aktualisiert.

### 10.2 Done-Kriterien
Eine Datatype-/Modellaenderung ist nur dann fertig, wenn:
1. Enum/Registry/Patterns konsistent aufloesen.
2. Kein mehrdeutiger Alias bestehende Typen ueberschreibt.
3. fail-closed Verhalten bei ungueltigen Eingaben erhalten bleibt.
4. Tests Regressionen fuer Detection und Mapping abdecken.
5. Doku-Landkarte fuer Entwickler und Verwender aktualisiert ist.

## 11. Kompatibilitaets- und Breaking-Change-Hinweise
| Aenderung | Risiko | Einordnung |
|---|---|---|
| Neuer `FileKind` ohne bestehende Signaturen zu aendern | meist additive Erweiterung | non-breaking, aber Test- und Doku-Pflicht |
| Alias aendert bestehendes Mapping | bestehende Consumer koennen anderes Ergebnis sehen | potentiell breaking |
| Oeffentliche Modellproperties entfernen/umbenennen | Binary/Source-Consumer brechen | breaking |
| Contract-Dokument (`docs/04_*`) nicht aktualisiert trotz Surface-Aenderung | Drift zwischen Code und Doku/Tests | release-blocking fuer API-Governance |

## 12. Nicht-Ziele
- Keine stillen API-Surface-Aenderungen ohne Contract-Dokumentation.
- Keine Sicherheitslockerung im Archivpfad ohne explizite Risikoentscheidung und Testnachweis.

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprueft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprueft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
