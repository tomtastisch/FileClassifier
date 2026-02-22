<!-- LANG_SWITCH:BEGIN -->
[DE](001_INDEX_TESTS.MD) | [EN](../../1_en/verification/001_INDEX_TESTS.MD)
<!-- LANG_SWITCH:END -->

# Testdokumentation (BDD)

## 1. Zweck
Diese Seite ist der Einstieg für die testbezogene SSOT-Dokumentation im Bereich BDD/Reqnroll.

## 2. Geltungsbereich
- `tests/FileTypeDetectionLib.Tests/Features/*.feature`
- `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs`
- Testlauf in CI via `tools/test-bdd-readable.sh`
- Runner/Adapter: Reqnroll mit `Reqnroll.xunit.v3` (xUnit v3)

## 3. Dokumente
1. [BDD-Satzkatalog (Deutsch, kanonisch)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/003_CATALOG_BDD.MD)
2. [BDD-Ausführung und Gherkin-Flow](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/002_FLOW_BDD.MD)

## 4. Verifikation
```bash
python3 tools/check-docs.py
bash tools/test-bdd-readable.sh
```

## 5. Verlinkte SSOT-Quellen
- [CI-Pipeline (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/ci/001_PIPELINE_CI.MD)
- [Dokumentationsstandard (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/004_POLICY_DOCUMENTATION.MD)
- [Readme](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/README.md)
