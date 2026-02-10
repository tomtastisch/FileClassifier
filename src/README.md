# Source-Artefakte - FileClassifier

## Zweck
Einstiegspunkt für die Source-Variante des Moduls.

## Inhalt
- [Program](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.App/Program.cs)
- [Fileclassifier.app](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.App/FileClassifier.App.csproj)
- [Anwendungsmodul Index](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.App/README.md)
- [Bibliotheksmodul Index](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Dokumentationsindex](https://github.com/tomtastisch/FileClassifier/blob/main/docs/001_INDEX_CORE.MD)
- [01 - Funktionen](https://github.com/tomtastisch/FileClassifier/blob/main/docs/010_API_CORE.MD)
- [02 - Gesamtarchitektur und Ablaufflüsse](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
- [03 - Referenzen](https://github.com/tomtastisch/FileClassifier/blob/main/docs/references/001_REFERENCES_CORE.MD)
- [DIN-orientierte Spezifikation (DE) - FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/main/docs/specs/001_SPEC_DIN.MD)
- [Index - Abstractions](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)
- [Abstractions Detection Index](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Detection/README.md)
- [Abstractions Archive Index](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Archive/README.md)
- [Abstractions Hashing Index](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Hashing/README.md)
- [Index - Configuration](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Configuration/README.md)
- [Index - Detection](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Detection/README.md)
- [Index - Infrastructure](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md)

## Strukturregel
Im Root von `FileTypeDetection` liegen die öffentlichen API-Dateien:
- `FileTypeDetector.vb`
- `ArchiveProcessing.vb`
- `FileMaterializer.vb`
- `FileTypeOptions.vb`
- `EvidenceHashing.vb`

Zusätzlich liegen dort Projekt-/Build-Artefakte wie `FileTypeDetectionLib.vbproj`, `packages.lock.json`, `README.md` sowie die Unterordner `Abstractions/`, `Configuration/`, `Detection/`, `Infrastructure/`.

## README-Regel
Jeder versionierte Quellordner unter `src/*` besitzt eine eigene `README.md` mit Verantwortungen und Verweisen.

## Synchronisation
Derzeit keine Repo-internen Sync-Skripte (portable/doc conventions).

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/010_API_CORE.MD` abgeglichen.
