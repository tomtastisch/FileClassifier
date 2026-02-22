<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/quality/001_CHECKLIST_PRODUCTION.MD) | [EN](001_CHECKLIST_PRODUCTION.MD)
<!-- LANG_SWITCH:END -->

# Production Readiness Checklist - FileTypeDetection

## 1. Build and Test Gate
- [ ] `dotnet restore FileClassifier.sln -v minimal`
- [ ] `python3 tools/check-docs.py` (checks local Markdown file links and heading anchors fail-closed)
- [ ] `dotnet format FileClassifier.sln --verify-no-changes`
- [ ] `dotnet build FileClassifier.sln --no-restore -warnaserror -v minimal`
- [ ] `dotnet test FileClassifier.sln -v minimal`
- [ ] Coverage-Gate: `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal /p:CollectCoverage=true /p:Include="[FileTypeDetectionLib]*" /p:CoverletOutputFormat=cobertura /p:Threshold=85%2c69 /p:ThresholdType=line%2cbranch /p:ThresholdStat=total`
- [ ] Increase coverage thresholds step-by-step (e.g. `85/69 -> 88/72 -> 90/75`) instead of jumping.
- [ ] Check BDD tag filters (e.g. `Category=e2e`, `Category=materializer`)

## 2. Security and Limits
- [ ] Set `FileTypeProjectBaseline.ApplyDeterministicDefaults()` at startup
- [ ] If needed: use `FileTypeOptions.LoadOptions(json)` for environment limits
- [ ] Align archive limits (`MaxZipEntries`, `MaxZipCompressionRatio`, `MaxZipNestingDepth`) with ops
- [ ] Validate root/traversal protections in the target system (filesystem permissions, container mounts)

## 3. Runtime Configuration
- [ ] Define configuration source per environment (embedded, file, secret/config service)
- [ ] Log a snapshot of active options via `FileTypeOptions.GetOptions()`
- [ ] Document the change process for limits (review + approval)

## 4. Operational Readiness
- [ ] Define logging level and destination (stdout/file/collector)
- [ ] Set up alerting for fail-closed spikes (Unknown/False/empty spikes)
- [ ] Establish throughput/latency baselines (e.g. benchmark smoke in CI)
- [ ] Provide an incident runbook for archive attack cases (bomb/traversal)

## 5. Integration Contract
- [ ] Clarify caller contract: do not expect exceptions as control flow
- [ ] Document return semantics:
  - `Unknown` for uncertain/invalid detection
  - `False` for failing guard/persistence paths
  - empty list for failed in-memory extraction
- [ ] Define consumer behavior on fail-closed in the service workflow

## 6. Release and Rollback
- [ ] Release version/changelog created
- [ ] Canary/staged rollout planned
- [ ] Rollback strategy defined (artifact + configuration + data paths)
- [ ] Post-release checks executed (smoke + critical BDD filters)

## 7. Quick Commands
```bash
dotnet restore FileClassifier.sln -v minimal
python3 tools/check-docs.py
dotnet format FileClassifier.sln --verify-no-changes
dotnet build FileClassifier.sln --no-restore -warnaserror -v minimal
dotnet test FileClassifier.sln -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal /p:CollectCoverage=true /p:Include="[FileTypeDetectionLib]*" /p:CoverletOutputFormat=cobertura /p:Threshold=85%2c69 /p:ThresholdType=line%2cbranch /p:ThresholdStat=total
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "Category=e2e" -v minimal
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "Category=materializer" -v minimal
```

## Documentation Maintenance Checklist
- [ ] Content verified against current code state.
- [ ] Links and anchors checked with `python3 tools/check-docs.py`.
- [ ] Examples/commands verified locally.
- [ ] Terminology aligned with `docs/110_API_CORE.MD`.
