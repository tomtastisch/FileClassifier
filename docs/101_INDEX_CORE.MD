# Documentation Index

## 1. Purpose
Central entry point for all project documentation, grouped by responsibility areas.

## 2. Standards and Guardrails
- Documentation standard (mandatory): [Documentation Standard (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/004_POLICY_DOCUMENTATION.MD)
- CI and quality evidence: [CI Pipeline (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/ci/001_PIPELINE_CI.MD)
- Auto labeling and auto versioning: [Auto Labeling and Auto Versioning (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/004_POLICY_LABELING.MD)
- Test documentation (BDD): [Test Documentation (BDD)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/001_INDEX_TESTS.MD)

## 3. Core Architecture (SSOT)
1. [01 - API Functions](https://github.com/tomtastisch/FileClassifier/blob/main/docs/010_API_CORE.MD)
2. [02 - Overall Architecture and Flows](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
3. [03 - References](https://github.com/tomtastisch/FileClassifier/blob/main/docs/references/001_REFERENCES_CORE.MD)
4. [04 - EvidenceHashing API Contract](https://github.com/tomtastisch/FileClassifier/blob/main/docs/contracts/001_CONTRACT_HASHING.MD)
5. [DIN-oriented Specification (DE) - FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/main/docs/specs/001_SPEC_DIN.MD)
6. [Production Readiness Checklist - FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/main/docs/quality/001_CHECKLIST_PRODUCTION.MD)
7. [Secure Hash Key Setup (HMAC)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/secure/001_HMAC_KEY_SETUP.MD)

## 4. Versioning and Governance
1. [Versioning and Release Policy (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/001_POLICY_VERSIONING.MD)
2. [Version Line (Commit -> Version)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/002_HISTORY_VERSIONS.MD)
3. [Changelog](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/003_CHANGELOG_RELEASES.MD)
4. [Labeling Ownership](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/002_POLICY_LABELING.MD)

## 5. Guides and Implementation Playbooks
1. [Guides - Change Playbooks](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/000_INDEX_GUIDES.MD)
2. [Playbook: Create and Adjust Options](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/001_GUIDE_OPTIONS.MD)
3. [Playbook: Extend New Datatypes and API Models](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/002_GUIDE_DATATYPE.MD)

## 5.1 Migration Notes
1. [Migration: Hashing Rename](https://github.com/tomtastisch/FileClassifier/blob/main/docs/migrations/001_HASHING_RENAME.MD)

## 6. Tests and Evidence Documents
1. [Test Matrix - Deterministic Hashing](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/004_MATRIX_HASHING.MD)
2. [BDD Catalog (German, canonical)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/003_CATALOG_BDD.MD)
3. [BDD Execution and Gherkin Flow](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/002_FLOW_BDD.MD)

## 7. Module-Local Documentation
- [FileTypeDetection Module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Index - Detection](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Detection/README.md)
- [Index - Infrastructure](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md)
- [Index - Configuration](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Configuration/README.md)
- [Index - Abstractions](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)
- [Abstractions Detection Module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Detection/README.md)
- [Abstractions Archive Module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Archive/README.md)
- [Abstractions Hashing Module](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Hashing/README.md)
- [README](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.App/README.md)
- [README](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/README.md)

## 8. Maintenance Process
Mandatory check for documentation changes:
```bash
python3 tools/check-docs.py
```

## Documentation Maintenance Checklist
- [ ] Content verified against current code state.
- [ ] Links and anchors checked with `python3 tools/check-docs.py`.
- [ ] Examples/commands verified locally.
- [ ] References to SSOT documents kept consistent.
