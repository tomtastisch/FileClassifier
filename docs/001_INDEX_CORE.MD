# Dokumentationsindex

## 1. Zweck
Zentrale Einstiegsstelle für alle Projektdokumente mit klarer Trennung nach Verantwortungsbereichen.

## 2. Standards und Leitplanken
- Dokumentationsstandard (verbindlich): [Dokumentationsstandard (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/governance/004_POLICY_DOCUMENTATION.MD)
- CI-/Qualitätsnachweise: [CI-Pipeline (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/ci/001_PIPELINE_CI.MD)
- Auto-Labeling und Auto-Versionierung: [Auto-Labeling und Auto-Versionierung (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/versioning/004_POLICY_LABELING.MD)
- Testdokumentation (BDD): [Testdokumentation (BDD)](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/tests/001_INDEX_TESTS.MD)

## 3. Kernarchitektur (SSOT)
1. [01 - Funktionen](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/010_API_CORE.MD)
2. [02 - Gesamtarchitektur und Ablaufflüsse](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/020_ARCH_CORE.MD)
3. [03 - Referenzen](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/references/001_REFERENCES_CORE.MD)
4. [04 - DeterministicHashing API Contract](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/contracts/001_CONTRACT_HASHING.MD)
5. [DIN-orientierte Spezifikation (DE) - FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/specs/001_SPEC_DIN.MD)
6. [Production Readiness Checklist - FileTypeDetection](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/quality/001_CHECKLIST_PRODUCTION.MD)

## 4. Versionierung und Governance
1. [Versionierungs- und Release-Policy (SSOT)](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/versioning/001_POLICY_VERSIONING.MD)
2. [Versionslinie (Commit → Version)](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/versioning/002_HISTORY_VERSIONS.MD)
3. [Changelog](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/versioning/003_CHANGELOG_RELEASES.MD)
4. [Labeling Ownership](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/governance/002_POLICY_LABELING.MD)

## 5. Guides und Umsetzungsplaybooks
1. [Guides - Change Playbooks](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/guides/000_INDEX_GUIDES.MD)
2. [Playbook: Options anlegen und anpassen](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/guides/001_GUIDE_OPTIONS.MD)
3. [Playbook: Neue Datatypes und API-Modelle erweitern](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/guides/002_GUIDE_DATATYPE.MD)

## 6. Test- und Nachweisdokumente
1. [Test Matrix - Deterministic Hashing](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/tests/004_MATRIX_HASHING.MD)
2. [BDD-Satzkatalog (Deutsch, kanonisch)](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/tests/003_CATALOG_BDD.MD)
3. [BDD-Ausfuehrung und Gherkin-Flow](https://github.com/tomtastisch/FileClassifier/blob/90a2825/docs/tests/002_FLOW_BDD.MD)

## 7. Modulnahe Dokumentation
- [FileTypeDetection Modul](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/README.md)
- [Index - Detection](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/Detection/README.md)
- [Index - Infrastructure](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/Infrastructure/README.md)
- [Index - Configuration](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/Configuration/README.md)
- [Index - Abstractions](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/Abstractions/README.md)
- [Abstractions Detection Modul](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/Abstractions/Detection/README.md)
- [Abstractions Archive Modul](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/Abstractions/Archive/README.md)
- [Abstractions Hashing Modul](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileTypeDetection/Abstractions/Hashing/README.md)
- [Readme](https://github.com/tomtastisch/FileClassifier/blob/90a2825/src/FileClassifier.App/README.md)
- [Readme](https://github.com/tomtastisch/FileClassifier/blob/90a2825/tests/FileTypeDetectionLib.Tests/README.md)

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
