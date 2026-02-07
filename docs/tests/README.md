# Testdokumentation (BDD)

## 1. Zweck
Diese Seite ist der Einstieg fuer die testbezogene SSOT-Dokumentation im Bereich BDD/Reqnroll.

## 2. Geltungsbereich
- `tests/FileTypeDetectionLib.Tests/Features/*.feature`
- `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs`
- Testlauf in CI via `tools/test-bdd-readable.sh`
- Runner/Adapter: Reqnroll mit `Reqnroll.xunit.v3` (xUnit v3)

## 3. Dokumente
1. [BDD_SATZKATALOG.md](./BDD_SATZKATALOG.md)
2. [BDD_EXECUTION_AND_GHERKIN_FLOW.md](./BDD_EXECUTION_AND_GHERKIN_FLOW.md)

## 4. Verifikation
```bash
python3 tools/check-docs.py
bash tools/test-bdd-readable.sh
```

## 5. Verlinkte SSOT-Quellen
- [../CI_PIPELINE.md](../CI_PIPELINE.md)
- [../DOCUMENTATION_STANDARDS.md](../DOCUMENTATION_STANDARDS.md)
- [../../tests/FileTypeDetectionLib.Tests/README.md](../../tests/FileTypeDetectionLib.Tests/README.md)
