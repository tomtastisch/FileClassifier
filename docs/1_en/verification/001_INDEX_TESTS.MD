<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/verification/001_INDEX_TESTS.MD) | [EN](001_INDEX_TESTS.MD)
<!-- LANG_SWITCH:END -->

# Test Documentation (BDD)

## 1. Purpose
This page is the entry point for test-related SSOT documentation in the BDD/Reqnroll area.

## 2. Scope
- `tests/FileTypeDetectionLib.Tests/Features/*.feature`
- `tests/FileTypeDetectionLib.Tests/Steps/FileTypeDetectionSteps.cs`
- CI test run via `tools/test-bdd-readable.sh`
- Runner/adapter: Reqnroll with `Reqnroll.xunit.v3` (xUnit v3)

## 3. Documents
1. [BDD catalog (German, canonical)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/103_CATALOG_BDD.MD)
2. [BDD execution and Gherkin flow](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/102_FLOW_BDD.MD)

## 4. Verification
```bash
python3 tools/check-docs.py
bash tools/test-bdd-readable.sh
```

## 5. Linked SSOT Sources
- [CI pipeline (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/ci/101_PIPELINE_CI.MD)
- [Documentation standard (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/104_POLICY_DOCUMENTATION.MD)
- [README](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/README.md)
