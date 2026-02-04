# Source-Artefakte - FileClassifier

## Zweck
Einstiegspunkt fuer die Source-Variante des Moduls.

## Inhalt
- [FileTypeDetection/README.md](./FileTypeDetection/README.md)
- [FileTypeDetection/docs/API_REFERENCE.md](./FileTypeDetection/docs/API_REFERENCE.md)
- [FileTypeDetection/docs/DIN_SPECIFICATION_DE.md](./FileTypeDetection/docs/DIN_SPECIFICATION_DE.md)
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
