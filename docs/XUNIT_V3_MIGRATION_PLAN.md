# xUnit v3 Migration Plan (Deterministic Rollout)

## 1. Goal and Scope
- Goal: Migrate the test toolchain from xUnit v2 to xUnit v3 with deterministic, revert-safe phases.
- In scope: test package graph migration, runner/discovery stabilization, CI and docs alignment, final policy-compliant version bump.
- Out of scope: runtime feature changes, public runtime API breaking changes, unrelated refactors.

## 2. Baseline Lock (Phase 0)
- Baseline date (UTC): 2026-02-07
- Baseline branch: `codex/migrate-xunit-v3` (created from `origin/main`)
- Baseline commit source: `origin/main` @ `fb780fc6dbeab528373d442107b2ce99fce7cdaf`

### 2.1 Baseline Metrics
- Scenario count (TRX): `385`
- Scenario pass/fail: `385/0`
- Coverage total (line/branch/method): `92.2% / 89.32% / 86.87%`
- Local restore runtime (`dotnet restore --locked-mode FileClassifier.sln`): `1.60s`
- Local build runtime (`dotnet build FileClassifier.sln --no-restore -warnaserror -v minimal`): `4.76s`
- Local test runtime (from test runner output): `532 ms` test execution duration

### 2.2 Baseline Evidence Artifacts
- `artifacts/migration/phase0/restore.log`
- `artifacts/migration/phase0/build.log`
- `artifacts/migration/phase0/tests/dotnet-test.log`
- `artifacts/migration/phase0/tests/results.trx`
- `artifacts/migration/phase0/test-readable.log`
- `artifacts/migration/phase0/coverage/coverage.cobertura.xml`
- `artifacts/coverage/coverage-summary.txt`

### 2.3 Baseline CI Status (main branch)
- Reference time (UTC): 2026-02-07
- `CI` workflow (run id `21782754721`): `success`
- `qodana` workflow (run id `21782754724`): `success`
- `Push on main` workflow (run id `21782754636`): `success`
- `Automatic Dependency Submission (NuGet)` (run id `21782754655`): `success`

## 3. Migration Gates (Hard Acceptance)
Each phase must satisfy all listed checks before commit:

1. `dotnet restore --locked-mode FileClassifier.sln` exits `0` (unless explicitly allowed in phase contract).
2. `dotnet build FileClassifier.sln --no-restore -warnaserror -v minimal` exits `0` (except Phase 1 documented compatibility fails).
3. `bash tools/test-bdd-readable.sh -- /p:CollectCoverage=true /p:Include="[FileTypeDetectionLib]*" /p:CoverletOutputFormat=cobertura /p:CoverletOutput="<out>/coverage" /p:Threshold=85%2c69 /p:ThresholdType=line%2cbranch /p:ThresholdStat=total` exits `0`.
4. Scenario-count parity: TRX `UnitTestResult` count must remain `385`, unless a justified deviation is documented.
5. Coverage gates remain satisfied (line >= 85 and branch >= 69).
6. Required CI checks remain unchanged and green: `preflight`, `build`, `security-nuget`, `tests-bdd-coverage`, `qodana`.

## 4. Deterministic Execution Contract
- One commit per completed phase.
- Commits are revert-safe (small, focused, independently testable).
- No squash commits during migration.
- Revert is commit-specific (`git revert <sha>`), never history rewrite.
- Any deviation is recorded in the Decision Log below.

## 5. Failure Analysis Template
Use this template for every migration failure event:

```markdown
### Failure Record <ID>
- Date (UTC):
- Phase:
- Signal:
- Command:
- Exit code:
- First failing artifact/log:
- Root cause hypothesis:
- Verification steps:
- Decision:
- Rollback action (if any):
- Follow-up commit(s):
```

## 6. Failure Modes and Rollback Strategy
- FM1 Restore conflict / incompatible package graph
  - Signal: `dotnet restore` fails.
  - Action: inspect dependency tree (`dotnet list package --include-transitive`), fix or revert only Phase-1 commit.
- FM2 Discovery regression (scenario count drops)
  - Signal: scenario count < `385`.
  - Action: revert Phase-2 commit, isolate runner/adapter config in a new focused commit.
- FM3 Compile errors in test code
  - Signal: test project build fails after package migration.
  - Action: isolate API break category, split compatibility changes into smaller commits, revert risky subset if needed.
- FM4 Coverage below gate
  - Signal: threshold failure (line < 85 or branch < 69).
  - Action: restore discovery/execution parity first, revert last risky commit when needed.
- FM5 Flaky CI
  - Signal: non-deterministic failures in repeated CI runs.
  - Action: apply conservative runner settings in separate stabilization commit; keep behavioral assertions unchanged.
- FM6 Versioning guard failure after bump
  - Signal: `tools/versioning/check-versioning.sh` fails.
  - Action: revert version bump commit and re-apply policy-compliant bump.

## 7. Decision Log
### DL-2026-02-07-01
- Decision: Use this document as migration SSOT for baseline, gates, and rollback protocol.
- Reason: enforce deterministic and auditable rollout for xUnit v3 migration.
- Evidence: Phase-0 baseline artifacts listed in section 2.2 and CI run status in section 2.3.
