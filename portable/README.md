# Portable-Artefakte - FileClassifier

## Zweck
Einstiegspunkt fuer die portable Variante des Moduls.

## Inhalt
- [FileTypeDetection/INDEX.md](./FileTypeDetection/INDEX.md)
- [FileTypeDetection/docs/API_REFERENCE.md](./FileTypeDetection/docs/API_REFERENCE.md)
- [FileTypeDetection/docs/DIN_SPECIFICATION_DE.md](./FileTypeDetection/docs/DIN_SPECIFICATION_DE.md)
- [FileTypeDetection/Abstractions/INDEX.md](./FileTypeDetection/Abstractions/INDEX.md)
- [FileTypeDetection/Configuration/INDEX.md](./FileTypeDetection/Configuration/INDEX.md)
- [FileTypeDetection/Detection/INDEX.md](./FileTypeDetection/Detection/INDEX.md)
- [FileTypeDetection/Infrastructure/INDEX.md](./FileTypeDetection/Infrastructure/INDEX.md)

## Strukturregel
Im Root von `FileTypeDetection` liegen nur:
- `FileTypeDetector.vb`
- `ZipProcessing.vb`
- `FileMaterializer.vb`
- `FileTypeOptions.vb`

## Synchronisation
- `bash tools/sync-portable-filetypedetection.sh`
- `bash tools/sync-doc-conventions.sh`
