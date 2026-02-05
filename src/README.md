# Source-Artefakte - FileClassifier

## Zweck
Einstiegspunkt fuer die Source-Variante des Moduls.

## Inhalt
- [FileClassifier.App/Program.cs](./FileClassifier.App/Program.cs)
- [FileClassifier.App/FileClassifier.App.csproj](./FileClassifier.App/FileClassifier.App.csproj)
- [FileTypeDetection/README.md](./FileTypeDetection/README.md)
- [docs/README.md](../docs/README.md)
- [docs/01_FUNCTIONS.md](../docs/01_FUNCTIONS.md)
- [docs/02_ARCHITECTURE_AND_FLOWS.md](../docs/02_ARCHITECTURE_AND_FLOWS.md)
- [docs/03_REFERENCES.md](../docs/03_REFERENCES.md)
- [docs/DIN_SPECIFICATION_DE.md](../docs/DIN_SPECIFICATION_DE.md)
- [FileTypeDetection/Abstractions/README.md](./FileTypeDetection/Abstractions/README.md)
- [FileTypeDetection/Configuration/README.md](./FileTypeDetection/Configuration/README.md)
- [FileTypeDetection/Detection/README.md](./FileTypeDetection/Detection/README.md)
- [FileTypeDetection/Infrastructure/README.md](./FileTypeDetection/Infrastructure/README.md)

## Strukturregel
Im Root von `FileTypeDetection` liegen nur:
- `FileTypeDetector.vb`
- `ZipProcessing.vb`
- `FileMaterializer.vb`
- `FileTypeOptions.vb`

## Synchronisation
- `bash tools/sync-portable-filetypedetection.sh`
- `bash tools/sync-doc-conventions.sh`
