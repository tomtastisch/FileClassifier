# Source-Artefakte - FileClassifier

## Zweck
Einstiegspunkt fuer die Source-Variante des Moduls.

## Inhalt
- [FileClassifier.App/Program.cs](./FileClassifier.App/Program.cs)
- [FileClassifier.App/FileClassifier.App.csproj](./FileClassifier.App/FileClassifier.App.csproj)
- [FileClassifier.App/README.md](./FileClassifier.App/README.md)
- [FileTypeDetection/README.md](./FileTypeDetection/README.md)
- [docs/README.md](../docs/README.md)
- [docs/01_FUNCTIONS.md](../docs/01_FUNCTIONS.md)
- [docs/02_ARCHITECTURE_AND_FLOWS.md](../docs/02_ARCHITECTURE_AND_FLOWS.md)
- [docs/03_REFERENCES.md](../docs/03_REFERENCES.md)
- [docs/DIN_SPECIFICATION_DE.md](../docs/DIN_SPECIFICATION_DE.md)
- [FileTypeDetection/Abstractions/README.md](./FileTypeDetection/Abstractions/README.md)
- [FileTypeDetection/Abstractions/Detection/README.md](./FileTypeDetection/Abstractions/Detection/README.md)
- [FileTypeDetection/Abstractions/Archive/README.md](./FileTypeDetection/Abstractions/Archive/README.md)
- [FileTypeDetection/Abstractions/Hashing/README.md](./FileTypeDetection/Abstractions/Hashing/README.md)
- [FileTypeDetection/Configuration/README.md](./FileTypeDetection/Configuration/README.md)
- [FileTypeDetection/Detection/README.md](./FileTypeDetection/Detection/README.md)
- [FileTypeDetection/Infrastructure/README.md](./FileTypeDetection/Infrastructure/README.md)

## Strukturregel
Im Root von `FileTypeDetection` liegen nur:
- `FileTypeDetector.vb`
- `ArchiveProcessing.vb`
- `FileMaterializer.vb`
- `FileTypeOptions.vb`

## README-Regel
Jeder versionierte Quellordner unter `src/*` besitzt eine eigene `README.md` mit Verantwortungen und Verweisen.

## Synchronisation
- `bash tools/sync-portable-filetypedetection.sh`
- `bash tools/sync-doc-conventions.sh`

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprueft.
- [ ] Links und Anker mit `python3 tools/check-markdown-links.py` geprueft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Begriffe mit `docs/01_FUNCTIONS.md` abgeglichen.
