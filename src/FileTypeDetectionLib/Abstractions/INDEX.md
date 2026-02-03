# Index - Abstractions

## 1. Purpose
Unveraenderliche Fachobjekte fuer stabile, portable Rueckgaben.

## 2. Inputs
- Werte aus Detection-/Infrastructure-Schicht

## 3. Outputs
- `DetectionDetail`
- `FileKind`
- `FileType`
- `ZipExtractedEntry`

## 4. Failure Modes / Guarantees
- `Unknown` bleibt verpflichtender fail-closed Typ.
- Objekte sind immutable und serialisierbar nutzbar.

## 5. Verification & Evidence
- Unit-Tests in `tests/FileTypeDetectionLib.Tests/Unit/`
