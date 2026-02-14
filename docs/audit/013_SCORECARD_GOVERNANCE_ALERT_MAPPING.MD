# Scorecard Governance Alert Mapping (Ruleset/Prozess)

Stand: 2026-02-13

## Verifizierter Ist-Stand (2026-02-13)
- Branch Protection `main`:
  - `required_pull_request_reviews.required_approving_review_count = 1`
  - `require_code_owner_reviews = true`
  - `require_last_push_approval = true`
- Fuzzing:
  - Workflow `.github/workflows/fuzzing-baseline.yml` aktiv.
  - Letzter manueller Nachweis-Run: `22003901268` (`success`).

## Ziel
Diese Datei mappt die repo-/governance-basierten Scorecard-Alerts ohne konkrete Datei (`no file associated`) auf verifizierbare Repository-Regeln und Prozesse.

## Scope
- `MaintainedID`
- `CodeReviewID`
- `BranchProtectionID`
- `FuzzingID`
- `CIIBestPracticesID`

## Nicht im Scope
- Codezeilen-Analyzer-Hinweise (z. B. `UseCollectionExpression`, `RedundantQualifier`).
- File-basierte Alerts (`TokenPermissionsID`, `PinnedDependenciesID`), da separat in Code/Workflow gefixt.

## Mapping (Claim -> Steuerung -> Evidence)

| Alert-ID | Steuerung (Soll) | Aktuelle Umsetzung | Evidence (deterministisch) | DoD A | DoD B |
|---|---|---|---|---|---|
| `BranchProtectionID` | `main` nur via PR + required checks | Branch Protection/Ruleset aktiv, required checks konfiguriert | `gh api repos/tomtastisch/FileClassifier/branches/main/protection` | `required_status_checks` vorhanden | direkter Push auf `main` technisch blockiert |
| `CodeReviewID` | Mindestens 1 PR-Review vor Merge | PR-Review-Policy in Branch Protection/Ruleset | `gh api repos/tomtastisch/FileClassifier/branches/main/protection --jq '.required_pull_request_reviews'` | `required_approving_review_count >= 1` | Merge ohne Review nicht möglich |
| `MaintainedID` | Nachweis aktiver Wartung | Kontinuierliche Commits/Releases + aktive CI | `gh api repos/tomtastisch/FileClassifier/commits?per_page=20` und `gh api repos/tomtastisch/FileClassifier/actions/runs?per_page=20` | in den letzten 90 Tagen Commits vorhanden | in den letzten 30 Tagen erfolgreiche Workflow-Runs vorhanden |
| `FuzzingID` | Fuzzing-Baseline vorhanden (mind. report-only) | Workflow `.github/workflows/fuzzing-baseline.yml` | `gh workflow view fuzzing-baseline.yml --yaml` und `gh run list --workflow fuzzing-baseline.yml --limit 10` | Workflow existiert und ist ausführbar | mindestens ein erfolgreicher Run in den letzten 30 Tagen |
| `CIIBestPracticesID` | Prozess-/Security-Baseline dokumentiert und nachvollziehbar | Audit-/Governance-Docs + CI-Evidence + Security Policy | `ls docs/audit` + `bash tools/audit/verify-security-claims.sh` | Audit-Index vorhanden und verlinkt | Security-Claims-Evidence liefert `pass` für Blocker-Claims |

## Hinweis zur Scorecard-Interpretation
- Diese Alerts bewerten Governance-Reife, nicht nur Codeinhalt.
- Ein Alert kann trotz technischer Teilumsetzung offen bleiben, wenn Scorecard öffentliche Signale anders gewichtet.
- Für Merge-Gates gelten nur intern deterministisch definierte Blocker-Checks.

## Operativer Prozess (fail-closed)
1. Wöchentlicher Snapshot der offenen Scorecard-Alerts.
2. Für jeden Governance-Alert Evidence-Befehl ausführen und Artefakt in `artifacts/security/` persistieren.
3. Bei Drift (`fail`) sofortige Korrektur in Ruleset/Workflow.
4. Nach Korrektur Re-Run von Scorecard + Security-Claims-Evidence.
