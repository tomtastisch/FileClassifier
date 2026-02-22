<!-- LANG_SWITCH:BEGIN -->
[DE](001_INDEX_CORE.MD) | [EN](../1_en/001_INDEX_CORE.MD)
<!-- LANG_SWITCH:END -->

# Dokumentationsindex

## 1. Zweck
Zentrale Einstiegsstelle für alle Projektdokumente mit klarer Trennung nach Verantwortungsbereichen.

## 2. Standards und Leitplanken
- Dokumentationsstandard (verbindlich): [Dokumentationsstandard (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/004_POLICY_DOCUMENTATION.MD)
- CI-/Qualitätsnachweise: [CI-Pipeline (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/ci/001_PIPELINE_CI.MD)
- Auto-Labeling und Auto-Versionierung: [Auto-Labeling und Auto-Versionierung (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/004_POLICY_LABELING.MD)
- Testdokumentation (BDD): [Testdokumentation (BDD)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/001_INDEX_TESTS.MD)

## 3. Kernarchitektur (SSOT)
1. [01 - Funktionen](https://github.com/tomtastisch/FileClassifier/blob/main/docs/010_API_CORE.MD)
2. [02 - Gesamtarchitektur und Ablaufflüsse](https://github.com/tomtastisch/FileClassifier/blob/main/docs/020_ARCH_CORE.MD)
3. [03 - Referenzen](https://github.com/tomtastisch/FileClassifier/blob/main/docs/references/001_REFERENCES_CORE.MD)
4. [04 - EvidenceHashing API Contract](https://github.com/tomtastisch/FileClassifier/blob/main/docs/contracts/001_CONTRACT_HASHING.MD)
5. [DIN-orientierte Spezifikation (DE) - FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/main/docs/specs/001_SPEC_DIN.MD)
6. [Production Readiness Checklist - FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/main/docs/quality/001_CHECKLIST_PRODUCTION.MD)
7. [Secure Hash Key Setup (HMAC)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/secure/001_HMAC_KEY_SETUP.MD)

## 4. Versionierung und Governance
1. [Versionierungs- und Release-Policy (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/001_POLICY_VERSIONING.MD)
2. [Versionslinie (Commit → Version)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/002_HISTORY_VERSIONS.MD)
3. [Changelog](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/003_CHANGELOG_RELEASES.MD)
4. [Labeling Ownership](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/002_POLICY_LABELING.MD)

## 5. Guides und Umsetzungsplaybooks
1. [Guides - Change Playbooks](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/000_INDEX_GUIDES.MD)
2. [Playbook: Options anlegen und anpassen](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/001_GUIDE_OPTIONS.MD)
3. [Playbook: Neue Datatypes und API-Modelle erweitern](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/002_GUIDE_DATATYPE.MD)

## 5.1 Migration Notes
1. [Migration: Hashing Rename](https://github.com/tomtastisch/FileClassifier/blob/main/docs/migrations/001_HASHING_RENAME.MD)

## 6. Test- und Nachweisdokumente
1. [Test Matrix - Deterministic Hashing](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/004_MATRIX_HASHING.MD)
2. [BDD-Satzkatalog (Deutsch, kanonisch)](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/003_CATALOG_BDD.MD)
3. [BDD-Ausführung und Gherkin-Flow](https://github.com/tomtastisch/FileClassifier/blob/main/docs/verification/002_FLOW_BDD.MD)

## 7. Modulnahe Dokumentation
- [FileTypeDetection Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/README.md)
- [Index - Detection](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Detection/README.md)
- [Index - Infrastructure](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Infrastructure/README.md)
- [Index - Configuration](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Configuration/README.md)
- [Index - Abstractions](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/README.md)
- [Abstractions Detection Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Detection/README.md)
- [Abstractions Archive Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Archive/README.md)
- [Abstractions Hashing Modul](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileTypeDetection/Abstractions/Hashing/README.md)
- [Readme](https://github.com/tomtastisch/FileClassifier/blob/main/src/FileClassifier.App/README.md)
- [Readme](https://github.com/tomtastisch/FileClassifier/blob/main/tests/FileTypeDetectionLib.Tests/README.md)

## 8. Pflegeprozess
Pflichtprüfung bei Doku-Änderungen:
```bash
python3 tools/check-docs.py
```

## Dokumentpflege-Checkliste
- [ ] Inhalt auf aktuellen Code-Stand geprüft.
- [ ] Links und Anker mit `python3 tools/check-docs.py` geprüft.
- [ ] Beispiele/Kommandos lokal verifiziert.
- [ ] Verweise auf SSOT-Dokumente konsistent gehalten.
