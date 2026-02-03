# Index - tests/FileTypeDetectionLib.Tests

## 1. Purpose
Deterministische Verifikation von Sicherheit, Korrektheit und Regressionen.

## 2. Inputs
- Testressourcen (`resources/`)
- oeffentliche API der Library

## 3. Outputs
- Teststatus, BDD-Ausgabe, Regressionsevidenz

## 4. Failure Modes / Guarantees
- Fehlverhalten blockiert Pipeline direkt.
- Sicherheitsregeln werden als automatisierte Assertions erzwungen.

## 5. Verification & Evidence
- `dotnet test FileClassifier.sln -v minimal`
- `bash tools/test-bdd-readable.sh`
