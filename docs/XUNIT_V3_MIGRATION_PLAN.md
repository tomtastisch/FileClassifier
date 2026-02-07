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

## 8. DoD (1:1 aus Auftrag)

```markdown
3-Phasen-Plan: CI Stabilisierung → PaC Light → xUnit v3 Migration
Ziel: langfristig tragbare, auditierbare und deterministische Pipeline + Test-Toolchain, ohne Toolchain-Sprawl.

PHASE 1 — CI Stabilisierung (Minimal-Core, Contract-First, Fail-Closed)
Outcome (Definition of Done)
- Alle Required Jobs erzeugen deterministisch Artefakte + result.json (schema-valid) unter EINEM SSOT-Pfad.
- CI ist fail-closed, drift-resistent (keine stillen Bypässe), Branch Protection bleibt stabil (Jobnamen unverändert).

1.1 SSOT-Fundament (Repo-Struktur + Docs)
- [x] tools/ci/ Struktur anlegen (bin/, lib/, policies/, schema/, checks/)
- [x] docs/governance/CI_PIPELINE.md anlegen (Layer/Jobs, Reihenfolge, Artefaktpfade)
- [x] docs/governance/CI_POLICY.md anlegen (Rule-ID-Katalog, Severity, Exit-Code-Matrix, Ausnahmen)
- [x] EIN Artifacts-Root als SSOT festlegen und dokumentieren:
      artifacts/ci/<check_id>/{raw.log,summary.md,result.json}
      artifacts/ci/qodana/{raw.log,summary.md,result.json,*.sarif}

1.2 Result-Contract (SSOT) + Validator
- [x] tools/ci/schema/result.schema.json definieren (schema_version=1, status, violations, timing UTC)
- [x] .NET Tool: ResultSchemaValidator implementieren
- [x] CI-Step: Schema-Validation required, fail-closed bei Schema-Verstoß (über summary/artifact contract)
- [x] result.json Konventionen festlegen:
      schema_version=1
      started_at/finished_at ISO-8601 UTC (…Z)
      duration_ms integer
      status pass|warn|fail
      severity warn|fail
      violation: rule_id + message + evidence_paths (min 1 bei fail)

1.3 Orchestrator-Helpers (Bash nur Orchestrierung)
- [x] tools/ci/lib/log.sh (einheitliche, deterministische Log-Ausgabe)
- [x] tools/ci/lib/result.sh (result.json writer + summary.md writer)
- [x] tools/ci/bin/run.sh (Entry-Point: run.sh <check_id> → ruft Checks/Policies, schreibt Artefakte)

1.4 Minimal Policies (Fail-Closed Enforcer)
- [x] policy_artifact_contract.sh:
      - prüft Pflichtartefakte pro check_id
      - missing => FAIL mit CI-ARTIFACT-001 + evidence_paths
- [x] policy_shell_safety.sh (eng definiert, evidence mit Zeilen):
      - continue-on-error: true => FAIL CI-SHELL-001
      - "|| true" in workflow YAML (kritische Pfade) => FAIL CI-SHELL-002
      - "set +e" ohne dokumentierte Ausnahme => FAIL CI-SHELL-003
      - Inline run block > 5 Zeilen => FAIL CI-SHELL-004

1.5 Workflow Refactor (kompatible Jobnamen)
- [x] .github/workflows/ci.yml refactoren: YAML nur Entry-Calls (bash tools/ci/bin/run.sh <check_id>)
- [x] Bestehende Required-Jobnamen unverändert lassen (Branch Protection stabil)
- [x] Jeder required job uploaded exakt:
      artifacts/ci/<check_id>/raw.log
      artifacts/ci/<check_id>/summary.md
      artifacts/ci/<check_id>/result.json

1.6 Negativtests (Proof of Governance)
- [x] fehlendes result.json erzwingen → CI-ARTIFACT-001 FAIL
- [x] ungültiges result.json erzwingen → CI-SCHEMA-001 FAIL
- [x] continue-on-error in Test-Branch → CI-SHELL-001 FAIL
- [x] Inline-run > 5 Zeilen → CI-SHELL-004 FAIL

---

PHASE 2 — PaC Light (maschinenlesbare Policies, evaluiert via .NET Tooling)

## Scope this PR (Phase 2.0)
- Fokus: **PaC Light Foundation** mit `PolicyRunner` + `artifact_contract`, `shell_safety` und `docs_drift`.
- Out of scope: `ci_graph` und `qodana_contract` als PolicyRunner-Evaluatoren.

## Outcome (Definition of Done)
- [x] Policies sind zentral als maschinenlesbare Dateien versioniert + schema-validiert.
- [x] CI evaluiert Policies deterministisch und mappt Ergebnisse in `result.json` (Rule-ID + Evidence) **für `artifact_contract`, `shell_safety` und `docs_drift`**.

## 2.1 Policy Files + Schema (SSOT)
- [x] `tools/ci/policies/rules/` angelegt
- [x] `tools/ci/policies/schema/` angelegt
- [x] rules-Konvention eingeführt (`rule_id`, `severity`, `title`, `description`, `applies_to`, `params`)
- [x] `params.binary_extensions` als konfigurierbare SSOT-Liste im Rules-Schema ergänzt (Erweiterungen ohne Codeänderung)

## 2.2 PolicyRunner (.NET) (PaC Light Engine)
- [x] `.NET` Tool `PolicyRunner` implementiert
  - [x] lädt rules + validiert gegen schema (fail-closed)
  - [x] führt Evaluator `artifact_contract` aus
  - [x] schreibt schema-valides `result.json` inkl. `violations` + `evidence_paths`
- [x] Invalid rules => `CI-POLICY-001` (fail)
- [x] Kein Rule-Match für `--check-id` => `CI-POLICY-001` (fail-closed, kein stiller Bypass)
- [x] `result.json` wird für `artifact_contract` wieder gegen `tools/ci/schema/result.schema.json` validiert (`CI-SCHEMA-001`)
- [x] Regex-Scanner überspringt Binär-/nicht-UTF8-Dateien deterministisch (`SCAN_SKIP`), kein Crash auf `tools/ci/checks/**/bin/**`
- [x] Binary-Filter wird aus Rule-Config gelesen (`binary_extensions`), nicht mehr hart im Code verdrahtet

## 2.3 Migration der Policies aus Bash zu Daten (minimal, iterativ)
- [x] `policy_artifact_contract`: Parameter (`required_artifacts`, `check_ids`) in Rule-Datei gezogen
- [x] `policy_shell_safety`: Schwellen/Patterns in Rule-Datei migriert und via PolicyRunner evaluiert
- [x] `summary`-Pfad auf dieselbe PolicyRunner-Bridge umgestellt (Hybridbetrieb entfernt)
- [x] `preflight` nutzt für `shell_safety` vollständig die PolicyRunner-Bridge (kein Bash-Policy-Hybrid mehr)
- [x] Legacy-Bridge-Skripte entfernt: `tools/ci/policies/policy_shell_safety.sh`, `tools/ci/policies/policy_artifact_contract.sh`
- [x] `CI_POLICY.md` bleibt human-readable und referenziert Rule-SSOT

## 2.4 Documentation & Ref-Links (No Redundancy, SSOT-only)
- [x] SSOT-Regel in `CI_POLICY.md` ergänzt (normativ: rules/schema)
- [x] Doku-Konsolidierung + POLICY_INDEX umgesetzt (SSOT-Links konsolidiert)
- [x] Drift-Guard `CI-DOCS-001` umgesetzt

## 2.5 Acceptance / Proof
- [x] invalide Rule-Datei => `CI-POLICY-001` fail
- [x] Rule violation erzeugt => deterministisches `result.json` mit `rule_id` + `evidence_paths`
- [x] Keine Änderung am Artifact-Contract/-Pfad

---

PHASE 3 — xUnit v3 Migration (deterministischer Rollout, revert-safe, versioniert)
Outcome (Definition of Done)
- Test-Toolchain läuft stabil auf xUnit v3 (inkl. BDD/Reqnroll Runner).
- Scenario-Count parity zur Baseline (oder begründete Abweichung dokumentiert).
- Coverage Gates erfüllt, CI required checks 2x in Folge grün.
- Version bump als finaler, policy-konformer Schritt.

3.1 Branching & Governance
- [ ] Branch: codex/migrate-xunit-v3 (Basis: origin/main frisch)
- [ ] Commit-Regel: ein Commit pro abgeschlossener Phase, revert-safe, keine Squashes während Migration
- [ ] Merge-Regel: erst nach 2 grünen CI-Läufen in Folge
- [ ] Required Checks (unverändert): preflight, build, security-nuget, tests-bdd-coverage, qodana

3.2 Phase 0: Baseline Lock & Gates
- [ ] docs/XUNIT_V3_MIGRATION_PLAN.md anlegen
- [ ] Baseline-Metriken erfassen und festhalten:
      - Scenario-Count (maschinenlesbar, z.B. aus TRX/Log parsebar)
      - Coverage (aktueller Schwellenwert + Ergebnis)
      - Laufzeit grob (CI/Local)
      - aktueller CI-Status
- [ ] Validations: dotnet restore/build, test-bdd-readable.sh (mit coverage), Qodana grün
- [ ] Commit: chore(migration): establish xunit v3 baseline and gates

3.3 Phase 1: Package Graph Migration (isoliert)
- [ ] Directory.Packages.props: xUnit2/Reqnroll.xUnit → v3-kompatible Pakete
- [ ] Test csproj References synchronisieren
- [ ] Keine Testlogik ändern
- [ ] Validation: dotnet restore grün; dotnet build darf erwartete API-Fails zeigen, aber protokolliert
- [ ] Commit: build(test): migrate package graph to xunit v3 stack
- [ ] Failure Gate: Restore-Konflikte → fix oder revert Phase-1-Commit

3.4 Phase 2: Adapter & Discovery Stabilisierung
- [ ] Runner/Discovery stabilisieren (test-bdd-readable.sh nur falls notwendig)
- [ ] Scenario-Count parity check: vor/nach identisch oder begründete Abweichung dokumentiert
- [ ] TRX + readable output vorhanden; Exit-Codes deterministisch
- [ ] Commit: test(ci): stabilize bdd discovery and runner output on xunit v3
- [ ] Failure Gate: Scenario-Count sinkt → revert Phase-2-Commit, Ursache isolieren

3.5 Phase 3: Mechanical Test Code Compatibility
- [ ] Nur mechanische Anpassungen (Namespaces, Attributes, Assertion API Unterschiede)
- [ ] Keine fachliche Testlogik ändern
- [ ] Validation: dotnet build grün; test-bdd-readable.sh grün; Coverage Gates erfüllt
- [ ] Commit: test: apply mechanical compatibility fixes for xunit v3
- [ ] Failure Gate: Behavioral Diffs → Commit splitten/revertieren, Root-Cause dokumentieren

3.6 Phase 4: CI/Doku Align (SSOT konsistent)
- [ ] ci.yml nur wenn zwingend für v3-Ausführung nötig
- [ ] Doku Update: README.md, CI_PIPELINE.md, docs/tests/*
- [ ] check-docs.py grün, docs-linkcheck grün
- [ ] Commit: docs(ci): align ssot docs and ci instructions for xunit v3

3.7 Phase 5: Hard Verification & Soak
- [ ] Vollständiger lokaler Quality-Run
- [ ] Zwei aufeinanderfolgende CI-Läufe grün
- [ ] Baseline-Vergleich:
      - Scenario-Count parity
      - Coverage >= Baseline
      - Keine neuen sicherheitsrelevanten Findings
- [ ] Optional Commit: chore(migration): record verification evidence

3.8 Phase 6: Policy-konforme Neuversionierung (final)
- [ ] Version bump gemäß POLICY.md / bestehendem Guard (SSOT: Directory.Build.props o.ä.)
- [ ] check-versioning.sh grün
- [ ] dotnet build + test-bdd-readable.sh grün
- [ ] CI grün
- [ ] Commit: chore(version): bump version for xunit v3 migration release

Stop Conditions (global)
- Wenn Contracts (artifacts Pfade, result.schema) brechen: sofort zurück zu Phase 1 Fix, keine Weiterarbeit an Phase 2/3.
- Wenn Migration Discovery/Coverage nicht deterministisch ist: Phase revertieren, Ursache isolieren, erst dann weiter.
```

## 9. Execution Status (Evidence-based)
- [x] Phase 0-4 committed with required commit messages (`844ade0`, `743b3e8`, `96b7550`, `5ceeac8`, `1742dae`)
- [x] Failure cause for CI preflight isolated: versioning-guard required `patch` on PR run `21782927706`
- [ ] Phase 5 soak (2 consecutive green CI runs) pending
- [ ] Phase 6 version bump pending
