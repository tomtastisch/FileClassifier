# Dokumentationsindex

## 1. Zweck
Zentrale Einstiegsstelle für alle Projektdokumente mit klarer Trennung nach Verantwortungsbereichen.

## 2. Standards und Leitplanken
- Dokumentationsstandard (verbindlich): [DOCUMENTATION_STANDARDS.md](./DOCUMENTATION_STANDARDS.md)
- CI-/Qualitätsnachweise: [CI_PIPELINE.md](./CI_PIPELINE.md)
- Auto-Labeling und Auto-Versionierung: [AUTO_LABELING_AND_VERSIONING.md](./AUTO_LABELING_AND_VERSIONING.md)
- Testdokumentation (BDD): [tests/README.md](./tests/README.md)

## 3. Kernarchitektur (SSOT)
1. [01_FUNCTIONS.md](./01_FUNCTIONS.md)
2. [02_ARCHITECTURE_AND_FLOWS.md](./02_ARCHITECTURE_AND_FLOWS.md)
3. [03_REFERENCES.md](./03_REFERENCES.md)
4. [04_DETERMINISTIC_HASHING_API_CONTRACT.md](./04_DETERMINISTIC_HASHING_API_CONTRACT.md)
5. [DIN_SPECIFICATION_DE.md](./DIN_SPECIFICATION_DE.md)
6. [PRODUCTION_READINESS_CHECKLIST.md](./PRODUCTION_READINESS_CHECKLIST.md)

## 4. Versionierung und Governance
1. [versioning/POLICY.md](./versioning/POLICY.md)
2. [versioning/VERSIONS.md](./versioning/VERSIONS.md)
3. [versioning/CHANGELOG.md](./versioning/CHANGELOG.md)
4. [governance/LABELING_OWNERSHIP.md](./governance/LABELING_OWNERSHIP.md)

## 5. Guides und Umsetzungsplaybooks
1. [guides/README.md](./guides/README.md)
2. [guides/OPTIONS_CHANGE_GUIDE.md](./guides/OPTIONS_CHANGE_GUIDE.md)
3. [guides/DATATYPE_EXTENSION_GUIDE.md](./guides/DATATYPE_EXTENSION_GUIDE.md)

## 6. Test- und Nachweisdokumente
1. [TEST_MATRIX_HASHING.md](./TEST_MATRIX_HASHING.md)
2. [tests/BDD_SATZKATALOG.md](./tests/BDD_SATZKATALOG.md)
3. [tests/BDD_EXECUTION_AND_GHERKIN_FLOW.md](./tests/BDD_EXECUTION_AND_GHERKIN_FLOW.md)

## 7. Modulnahe Dokumentation
- [../src/FileTypeDetection/README.md](../src/FileTypeDetection/README.md)
- [../src/FileTypeDetection/Detection/README.md](../src/FileTypeDetection/Detection/README.md)
- [../src/FileTypeDetection/Infrastructure/README.md](../src/FileTypeDetection/Infrastructure/README.md)
- [../src/FileTypeDetection/Configuration/README.md](../src/FileTypeDetection/Configuration/README.md)
- [../src/FileTypeDetection/Abstractions/README.md](../src/FileTypeDetection/Abstractions/README.md)
- [../src/FileTypeDetection/Abstractions/Detection/README.md](../src/FileTypeDetection/Abstractions/Detection/README.md)
- [../src/FileTypeDetection/Abstractions/Archive/README.md](../src/FileTypeDetection/Abstractions/Archive/README.md)
- [../src/FileTypeDetection/Abstractions/Hashing/README.md](../src/FileTypeDetection/Abstractions/Hashing/README.md)
- [../src/FileClassifier.App/README.md](../src/FileClassifier.App/README.md)
- [../tests/FileTypeDetectionLib.Tests/README.md](../tests/FileTypeDetectionLib.Tests/README.md)

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
