# CI-Pipeline (SSOT)

## 1. Zweck
Diese Pipeline liefert auditierbare Nachweise für Produktionsreife:
- Konsistenz/Format
- Build-Korrektheit (`--warnaserror`)
- Paket-Sicherheit (NuGet-Vulnerabilities)
- BDD-Readable-Tests mit Coverage-Gate
- Doku-Konsistenz
- deterministisches PR-Labeling und Versionierungs-Nachweise

## 2. Workflow-Struktur
Der Workflow `CI` hat zwei klar getrennte Verantwortungswege:
- `pull_request`: `pr-labeling` (Governance, nicht-gating)
- `pull_request`/`push`: technische Qualitätsjobs (`preflight`, `build`, `security-nuget`, `tests-bdd-coverage`, `summary`)

## 3. Jobs (1 Job = 1 Verantwortung)
### Job: pr-labeling (Governance)
- geänderte Dateien und aktuelle PR-Labels erfassen
- `required/actual/reason` aus Versioning-Guard ableiten
- deterministische Label-Entscheidung berechnen (`decision.json`)
- Schema validieren
- veraltete Auto-Labels entfernen und neue setzen
- Artefakte hochladen

### Job: preflight (fail-fast)
1. Label-Engine Golden-Tests
2. Docs-Check
3. Versioning-Guard
4. Format-Check

### Job: build
1. Restore
2. Build (`--warnaserror`)

### Job: security-nuget
1. Vulnerability-Scan (`--include-transitive`)
2. Deprecated-Packages-Report

### Job: tests-bdd-coverage
1. Single-Run BDD-Tests + Coverage-Gate (`Line >= 85`, `Branch >= 69`)

### Job: summary
- Coverage- und Security-Zusammenfassung (nur Reporting)

## 4. Required vs. Non-Required Checks
Required in Branch-Protection:
- `preflight`
- `build`
- `security-nuget`
- `tests-bdd-coverage`
- optional: `qodana` (policy-abhängig)

Nicht required:
- `pr-labeling` (Governance-Automation, fail-open)
Wenn Label-API-Aufrufe fehlschlagen, wird dies als Artefakt protokolliert, ohne technische Quality-Gates zu brechen.

## 5. Stabile Artefaktpfade
- `artifacts/labels/decision.json`
- `artifacts/docs/doc-check.txt`
- `artifacts/versioning/versioning-check.txt`
- `artifacts/format/format-check.txt`
- `artifacts/build/build-log.txt`
- `artifacts/security/nuget-vuln.txt`
- `artifacts/security/nuget-deprecated.txt`
- `artifacts/tests/**` (TRX, readable output, raw test log)
- `artifacts/coverage/coverage.cobertura.xml`
- `artifacts/coverage/coverage-summary.txt`

## 6. Labeling- und Versioning-SSOT
- Policy: `docs/versioning/POLICY.md`
- Verhalten + Diagramme: `docs/AUTO_LABELING_AND_VERSIONING.md`
- Ownership: `docs/governance/LABELING_OWNERSHIP.md`

## 7. Qodana
Qodana bleibt ein separater Workflow und ergänzt die CI-Qualitätsgates.
Coverage-SSOT bleibt CI (Coverage-Artefakte + Gate-Enforcement).
Für `pull_request` wird SARIF als Workflow-Artefakt veröffentlicht.
Code-Scanning-SARIF-Upload erfolgt nur auf non-PR-Runs, um PR-Noise zu vermeiden.

## 8. Lokale Reproduktion
```bash
node tools/versioning/test-compute-pr-labels.js
python3 tools/check-docs.py
bash tools/versioning/check-versioning.sh
dotnet format FileClassifier.sln --verify-no-changes
dotnet restore FileClassifier.sln -v minimal
dotnet build FileClassifier.sln --no-restore -warnaserror -v minimal

dotnet list FileClassifier.sln package --vulnerable --include-transitive
dotnet list FileClassifier.sln package --deprecated

TEST_BDD_OUTPUT_DIR=artifacts/tests bash tools/test-bdd-readable.sh -- \
  /p:CollectCoverage=true \
  /p:Include="[FileTypeDetectionLib]*" \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput="$(pwd)/artifacts/coverage/coverage" \
  /p:Threshold=85%2c69 \
  /p:ThresholdType=line%2cbranch \
  /p:ThresholdStat=total
```
