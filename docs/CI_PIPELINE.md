# CI Pipeline - FileClassifier

## 1. Purpose
This CI pipeline provides auditable production-readiness evidence for:
- consistency/format
- build correctness (`warnaserror`)
- dependency security (NuGet vulnerabilities)
- BDD-readable tests with coverage gate
- documentation consistency
- deterministic PR auto-labeling and auto-versioning evidence

## 2. Workflow Structure
The `CI` workflow contains two separated responsibility paths:
- `pull_request_target` path: `pr-labeling` (label governance only)
- `pull_request`/`push` path: technical quality jobs (`preflight`, `build`, `security-nuget`, `tests-bdd-coverage`, `summary`)

## 3. Jobs (1 job = 1 concern)
### Job: pr-labeling (governance)
- Collect changed files and current PR labels
- Derive `required/actual/reason` from versioning guard
- Compute deterministic labels (`decision.json`)
- Validate schema
- Remove stale auto-labels and apply new labels
- Upload labeling artifact

### Job: preflight (fail-fast)
1. Label engine golden tests
2. Docs check
3. Versioning guard
4. Format check

### Job: build
1. Restore
2. Build (`--warnaserror`)

### Job: security-nuget
1. Vulnerability scan (`--include-transitive`)
2. Deprecated package report

### Job: tests-bdd-coverage
1. Single-run BDD tests + coverage gate (`Line >= 85`, `Branch >= 69`)

### Job: summary
- Coverage and security summary (report-only)

## 4. Required vs Non-Required Checks
Required in branch protection:
- `preflight`
- `build`
- `security-nuget`
- `tests-bdd-coverage`
- optional: Qodana (policy-dependent)

Not required:
- `pr-labeling` (governance automation, non-gating)

## 5. Stable Artifact Paths
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

## 6. Labeling and Versioning SSOT
- Policy: `docs/versioning/POLICY.md`
- Full behavior + charts: `docs/AUTO_LABELING_AND_VERSIONING.md`
- Ownership: `docs/governance/LABELING_OWNERSHIP.md`

## 7. Qodana
Qodana remains a separate workflow and complements CI quality gates.
Coverage source of truth remains CI coverage artifacts and gate enforcement.

## 8. Local Reproduction
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
