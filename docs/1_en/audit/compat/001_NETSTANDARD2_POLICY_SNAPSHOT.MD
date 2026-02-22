<!-- LANG_SWITCH:BEGIN -->
[DE](../../../0_de/audit/compat/001_NETSTANDARD2_POLICY_SNAPSHOT.MD) | [EN](001_NETSTANDARD2_POLICY_SNAPSHOT.MD)
<!-- LANG_SWITCH:END -->

# Policy Snapshot: netstandard2 Compat Refactor

## 1. Zweck
Dieses Dokument extrahiert verbindliche Anforderungen fuer den netstandard2.0-Kompatibilitaetsrefactor.

## 2. Geltungsbereich
- Quelle A: `docs/governance/045_CODE_QUALITY_POLICY_DE.MD`
- Quelle B: `docs/versioning/001_POLICY_VERSIONING.MD`
- Quelle C: `docs/governance/004_POLICY_DOCUMENTATION.MD`

## 3. Regeln/Architektur
### 3.1 Muss (verbindlich)
- Keine Public-Signaturaenderungen und kein Behavior-Drift in bestehenden Public APIs.
- Build/Test muessen erfolgreich sein.
- Fail-closed Verhalten (Fehlerpfade und Logging) darf nicht aufgeweicht werden.
- Versionierungsentscheidung muss SemVer-konform sein.
- Dokumentdateien unter `docs/` muessen `UPPER_SNAKE_CASE` mit `.MD` nutzen.
- Doku-Checks sind verpflichtend:
  - `python3 tools/check-doc-consistency.py`
  - `python3 tools/check-docs.py`

### 3.2 Kann (erlaubt unter Bedingungen)
- Interne Refactorings und Strukturverbesserungen sind erlaubt, wenn Public API und Semantik erhalten bleiben.
- Interne Abstraktionen fuer TFM-sensitive APIs sind erlaubt.

### 3.3 Nicht erlaubt
- Runtime-Provider-Switching oder environment-basierte Pfadwahl.
- Unbelegte Annahmen ohne Evidence.
- Nicht-policy-konforme Dokumentbenennung.

## 4. Verifikation/Nachweise
Auszuege der Policyquellen:
- `docs/governance/045_CODE_QUALITY_POLICY_DE.MD:36-39` (keine Semantikaenderung, keine Signaturaenderung)
- `docs/governance/045_CODE_QUALITY_POLICY_DE.MD:196-204` (DoD Build/Test, keine Public-Signature-Aenderung)
- `docs/versioning/001_POLICY_VERSIONING.MD:22-26` (MINOR/PATCH Klassifikation)
- `docs/governance/004_POLICY_DOCUMENTATION.MD:16-24` (Benennungsregeln)
- `docs/governance/004_POLICY_DOCUMENTATION.MD:55-59` (Doku-Pflichtchecks)

## 5. Grenzen/Nicht-Ziele
- Keine Aenderung an `SECURITY.md`.
- Keine Erweiterung der fachlichen Semantik.
- Keine Einfuehrung neuer externer Runtime-Abhaengigkeiten ausser zur Kompatibilitaet notwendige Library-Referenzen.

## 6. Verlinkte SSOT-Quellen
- [Code Quality Policy DE](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/045_CODE_QUALITY_POLICY_DE.MD)
- [Versioning Policy DE](https://github.com/tomtastisch/FileClassifier/blob/main/docs/versioning/001_POLICY_VERSIONING.MD)
- [Documentation Policy DE](https://github.com/tomtastisch/FileClassifier/blob/main/docs/governance/004_POLICY_DOCUMENTATION.MD)

## RoC-Bezug
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
- [Shell-Safety-Regeln](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/shell_safety.yaml)
