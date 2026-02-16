<!-- LANG_SWITCH:BEGIN -->
[DE](002_POLICY_LABELING.MD) | [EN](102_POLICY_LABELING.MD)
<!-- LANG_SWITCH:END -->

# Labeling-Verantwortung

## 1. Geltungsbereich
Diese Governance-Richtlinie gilt fuer deterministisches PR-Auto-Labeling und Auto-Versionierungsverhalten.

## 2. Verantwortete Dateien
- `.github/workflows/ci.yml`
- `tools/versioning/*`
- `docs/versioning/001_POLICY_VERSIONING.MD`
- `docs/versioning/004_POLICY_LABELING.MD`

## 3. Aenderungsanforderungen
Jede Aenderung an Taxonomie, Prioritaet, Caps oder Versionierungsentscheidungslogik muss enthalten:
- aktualisierte Dokumentation
- aktualisierte/neu hinzugefuegte Golden-Testfaelle
- erfolgreiche Label-Engine-Validierung

## 4. Review-Policy
Mindestens ein Maintainer-Owner-Review ist fuer die verantworteten Dateien erforderlich.

## 5. Nicht-Ziele
Diese Richtlinie aendert nicht das Laufzeitverhalten des Produkts; sie regelt ausschliesslich Repository-Automation.

## RoC-Bezug
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
- [Shell-Safety-Regeln](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/shell_safety.yaml)
