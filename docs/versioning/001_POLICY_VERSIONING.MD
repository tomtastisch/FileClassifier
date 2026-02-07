# Versionierungs- und Release-Policy (SSOT)

## 1. Zweck
Diese Policy definiert verbindliche SemVer-Regeln (`MAJOR.MINOR.PATCH`) und die deterministische CI-Erzwingung.

## 2. Geltungsbereich
- Produkt- und Bibliotheksversionen aus `Directory.Build.props` (einzige SSOT-Quelle).
- PR-Versioning-Guard und Auto-Labeling in CI.

## 3. Begriffe
- SSOT: Single Source of Truth fuer Versioninformation.
- Breaking Change: Aenderung, die bestehende Integrationen ohne Anpassung brechen kann.

## 4. SemVer-Regeln (verbindlich)
- `MAJOR`
  - oeffentliche API entfernt/umbenannt
  - inkompatibles Standardverhalten
- `MINOR`
  - neue abwaertskompatible Features oder API-Erweiterungen
- `PATCH`
  - Fixes, Refactorings ohne Verhaltensbruch, Tests, Doku, CI, Tooling
- `NONE`
  - reine Metadatenaenderungen ohne Versionsauswirkung

## 5. Entscheidungs-Matrix
| Aenderungstyp | Beispiel | Bump |
|---|---|---|
| Public API rename/remove | `ZipProcessing` -> `ArchiveProcessing` | MAJOR |
| Breaking Defaults/Policy | stricteres Verhalten ohne Opt-in | MAJOR |
| Kompatibles Feature | neuer Datentyp/Flow | MINOR |
| Fix/Refactor | Stabilisierung, Cleanup | PATCH |
| Doku/Test/CI/Tooling | Workflow-, Skript-, Test-, Doku-Update | PATCH |
| Metadaten-only | mechanische Aenderung ohne Laufzeitwirkung | NONE |

## 6. CI-Guard (Pflicht)
- `tools/versioning/check-versioning.sh` klassifiziert Aenderungen und prueft den erforderlichen Bump.
- `versioning-guard` muss vor Build/Test grün sein.
- Nachweis in Logs/Summary:
  - `required=<...>`
  - `actual=<...>`
  - `reason=<...>`

## 7. Deterministisches Auto-Labeling (Pflicht)
Caps pro PR-Lauf:
- genau ein `version:*`
- genau ein Primary-Label
- maximal ein `impl:*`
- maximal zwei `area:*`

Alle veralteten Auto-Labels werden vor dem Anwenden neuer Labels entfernt.

## 8. Label-Registry
### 8.1 Version-Labels
- `version:major`
- `version:minor`
- `version:patch`
- `version:none`

### 8.2 Primary-Labels
- `breaking`, `feature`, `fix`, `refactor`, `ci`, `test`, `docs`, `tooling`, `chore`

### 8.3 Implementation-Labels
- `impl:quality`, `impl:security`, `impl:docs`, `impl:config`

### 8.4 Area-Labels
- `area:pipeline`, `area:qodana`, `area:archive`, `area:hashing`, `area:detection`,
- `area:materializer`, `area:versioning`, `area:tests`, `area:docs`, `area:tooling`

Kanonische Quelle:
- `tools/versioning/labels.json`

## 9. Prioritaetsregel
Primary-Prioritaet ist fix:
`breaking > feature > fix > refactor > ci > test > docs > tooling > chore`

## 10. Zugehoerige SSOT-Dokumente
- `docs/versioning/004_POLICY_LABELING.MD`
- `docs/ci/001_PIPELINE_CI.MD`
- `docs/governance/LABELING_OWNERSHIP.md`
- `docs/governance/004_POLICY_DOCUMENTATION.MD`

## 11. Qodana
Qodana ist zusaetzliche Static-Analysis und ersetzt keine CI-Quality-Gates fuer Build/Test/Coverage/Versioning.

## RoC-Bezug
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/tools/ci/policies/rules/docs_drift.yaml)
- [Shell-Safety-Regeln](https://github.com/tomtastisch/FileClassifier/blob/241c6d4/tools/ci/policies/rules/shell_safety.yaml)
