<!-- LANG_SWITCH:BEGIN -->
[DE](003_CHANGELOG_RELEASES.MD) | [EN](103_CHANGELOG_RELEASES.MD)
<!-- LANG_SWITCH:END -->

# Changelog

All changes are documented here in technical terms. The release version itself is the Git tag `vX.Y.Z` (optional `-prerelease`) as SSOT.

## [6.1.13]
- Added:
  - Completed German language unification for remaining CSCore XML in-code documentation areas (`Model`, `Mapping`, `Utilities`).
- Changed:
  - Consolidated C# XML comment terminology (for example `Nutzlast`, `Aufzaehlungssequenzen`, `Protokollierungspfade`) while preserving technical identifiers (for example `<c>ReasonCode</c>`, `<c>Unknown</c>`).
  - Applied consistent readability structure in comments (`<b>...</b>` for emphasis, `<br/>` for hover-friendly line wraps).
- Docs/CI/Tooling:
  - Version convergence set to `6.1.13` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.12]
- Added:
  - Introduced the canonical bilingual documentation grid in `docs/0_de` and `docs/1_en` with identical file sets per language.
  - Extended sync tooling `tools/docs/sync_bilingual_structure.py` with fail-closed language-switch-header validation.
- Changed:
  - Harmonized EN versioning docs (`102/103`) to the current 6.x release state.
  - Sync tooling now deterministically prunes stale target files, validates file parity, and enforces switch blocks in both language trees.
  - Expanded PR-scope allowlist for the new grid paths (`docs/0_de/**`, `docs/1_en/**`) including language-switch report files.
- Docs/CI/Tooling:
  - Completed full local required checks end-to-end (`preflight` through `summary`) as evidence.
  - Version convergence set to `6.1.12` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.11]
- Added:
  - New CSCore utility `ArchivePathPolicyUtility` for archive-relative-path normalization and root-path policy.
  - New bridge operations `NormalizeArchiveRelativePath` and `IsRootPath`.
- Changed:
  - `ArchiveEntryPathPolicy.TryNormalizeRelativePath` now delegates to CSCore first and falls back fail-closed to VB.
  - `DefaultDestinationPathPolicy.IsRootPath` now delegates to CSCore first and falls back fail-closed to VB.
  - `CsCoreUtilityBridgeUnitTests` extend telemetry coverage for archive/path policy delegation.
  - Package-backed contract checks now assert presence of `ArchivePathPolicyUtility`.
  - CSCore utility docs and DE/EN references were extended for archive/path policy.
- Docs/CI/Tooling:
  - Version convergence set to `6.1.11` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.10]
- Added:
  - New CSCore utility `EvidencePolicyUtility` for hash-evidence policy (label normalization, note append, HMAC env-key resolution).
  - New bridge operations `NormalizeEvidenceLabel`, `AppendNoteIfAny`, and `ResolveHmacKeyFromEnvironment`.
- Changed:
  - `EvidenceHashingCore.NormalizeLabel`, `AppendNoteIfAny`, and `TryResolveHmacKey` now delegate to CSCore first with deterministic fail-closed VB fallback.
  - `CsCoreUtilityBridgeUnitTests` extend telemetry/delegation coverage for evidence-policy paths.
  - Package-backed contract checks now assert presence of `EvidencePolicyUtility`.
  - CSCore utility docs and DE/EN references were extended for evidence policy.
- Docs/CI/Tooling:
  - Version convergence set to `6.1.10` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.9]
- Added:
  - New CSCore utility `OfficePolicyUtility` for OpenXML/OpenDocument and legacy Office decision logic.
  - Bridge operations `ResolveOpenDocumentMimeKindKey`, `ResolveArchivePackageKindKey`, and `ResolveLegacyMarkerKindKey`.
- Changed:
  - `OpenXmlRefiner` delegates MIME/package decisions to CSCore first with fail-closed VB fallback.
  - `LegacyOfficeBinaryRefiner` delegates marker decisions to CSCore first with fail-closed VB fallback.
  - `CsCoreUtilityBridgeUnitTests` validate office-policy delegation paths including telemetry counters.
  - CSCore utility docs and DE/EN references were extended for office policy.
- Docs/CI/Tooling:
  - Version convergence set to `6.1.9` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.8]
- Added:
  - New CSCore utility `DetectionPolicyUtility` for extension policy, reason-code mapping, and detection-summary projection.
  - Complete CSCore README coverage for root, `Model`, `Mapping`, and `Utilities`.
  - Bridge operations `BuildSummaryValues`, `IsExtensionMatch`, `ExceptionToReasonCode` with telemetry counting.
- Changed:
  - `FileTypeDetector.DetectDetailed` and `DetectAndVerifyExtension` now use projected CSCore summary context with fail-closed VB fallback.
  - `FileTypeDetector.ExceptionToReasonCode` now delegates to CSCore first with deterministic VB fallback.
  - `CsCoreUtilityBridgeUnitTests` validate delegation for `BuildSummaryValues`, `IsExtensionMatch`, and `ExceptionToReasonCode`.
  - In-code CSCore XML documentation was completed for models, mappers, and utilities.
- Docs/CI/Tooling:
  - DE/EN references extended for CSCore bridge/policy paths and compile-time dependencies (`Riok.Mapperly`, `PolySharp`).
  - Version convergence set to `6.1.8` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.7]
- Added:
  - Dependency canary now includes `System.Text.Encoding.CodePages` (risk scope: `archive-text-encoding`).
  - BDD step alias `JPG -> Jpeg` for stable legacy feature compatibility.
- Changed:
  - Analyzer major upgrade: `Meziantou.Analyzer` to `3.0.12`, `Microsoft.CodeAnalysis.NetAnalyzers` to `10.0.103`.
  - Added direct centrally pinned dependency `System.Text.Encoding.CodePages` (`10.0.3`) for `netstandard2.0`.
  - Excluded `CsCoreRuntimeBridge` from coverage metric focus so BDD gates keep evaluating productive logic instead of reflection bridge plumbing.
  - Standardized local `6.x` unreleased history links to stable `compare/main...HEAD` anchors.
- Docs/CI/Tooling:
  - DE/EN supply-chain baseline extended with netstandard2.0 dependency strategy (CodePages pin + documented Microsoft.NETCore.Platforms meta exception).
  - Full local `run.sh` end-to-end run (`preflight` to `summary`) completed successfully.
  - Version convergence set to `6.1.7` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.6]
- Added:
  - `PolySharp` in CSCore for compile-time generated `IsExternalInit` compatibility (records/init support without manual polyfill files).
  - Fail-closed banned-API rules extended for unsafe direct ZIP extraction APIs (`ZipFile.ExtractToDirectory*`, `ZipFileExtensions.ExtractToFile*`).
- Changed:
  - Upgraded `DotNet.ReproducibleBuilds` to `2.0.2`.
- Docs/CI/Tooling:
  - Updated DE/EN references for compile-time codegen hardening and banned API policy.
  - Version convergence set to `6.1.6` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.5]
- Added:
  - Introduced `DotNet.ReproducibleBuilds` as centralized package baseline.
- Changed:
  - Activated deterministic reproducible-build defaults across the solution.
- Docs/CI/Tooling:
  - Updated DE/EN references and governance docs for reproducible build evidence.
  - Version convergence set to `6.1.5` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.4]
- Added:
  - Extended analyzer and banned-API baseline across relevant C# projects.
  - Added CSCore options normalization via records + Mapperly compile-time mapping.
- Changed:
  - Productive options-normalization path now delegates to shared CSCore logic instead of duplicated mapping code.
- Docs/CI/Tooling:
  - Extended DE/EN references for analyzer/banned-API policy and CSCore offloading.
  - Version convergence set to `6.1.4` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.3]
- Added:
  - Additional CSCore offloading for hashing/materializer logic.
- Changed:
  - Productive VB paths consume CSCore utilities with fail-closed runtime fallback.
  - Decision-matrix regression and telemetry/audit snapshot checks were expanded.
- Docs/CI/Tooling:
  - Extended DE/EN references and audit docs for the new offloading scope.
  - Version convergence set to `6.1.3` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.2]
- Added:
  - Runtime bridge operations for enum/iterable/argument/exception-filter utility offloading.
- Changed:
  - Package-backed contract hardening expanded to bridge surface checks.
- Docs/CI/Tooling:
  - Added DE/EN references for FC-0005 start and utility-bridge policy.
  - Version convergence set to `6.1.2` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.1.1]
- Added:
  - CSCore records and Mapperly pilot with compile-time mapping.
- Changed:
  - Enabled `IsExternalInit` compatibility path for netstandard consumers.
- Docs/CI/Tooling:
  - Added DE/EN governance and reference evidence for FC-0004 completion.
  - Version convergence set to `6.1.1` (`RepoVersion`, `Version`, `PackageVersion`, DE/EN version history).

## [6.0.0]
- Added:
  - New internal hashing services (`EvidenceHashingCore`, `EvidenceHashingRoundTrip`, `EvidenceHashingIo`) for deterministic extraction without adding dependencies.
- Changed:
  - Breaking migration of `FileKind` values (`Docx`/`Xlsx`/`Pptx` -> `Doc`/`Xls`/`Ppt`).
  - Breaking migration of `HashRoundTripReport` public API to a slot-based model (`HashSlot`, `Evidence(...)`, `LogicalEquals(...)`, `PhysicalEquals(...)`).
  - Reduced `EvidenceHashing` to a facade and moved core logic into internal stateless services.
- Fixed:
  - Updated reflection-based unit tests to the new internal hashing types.
- Docs/CI/Tooling:
  - Set active version convergence to `6.0.0` (`RepoVersion`, `Version`, `PackageVersion`, version history DE/EN).

## [5.2.1]
- Added:
  - Extended CI documentation to state Qodana as an explicit mandatory gate.
- Changed:
  - Switched SharpCompress calls in `ArchiveInternals.vb` to type-safe APIs (`ArchiveFactory.OpenArchive`, `GZipArchive.OpenArchive`).
  - Hardened `qodana.yml` fail-closed behavior: removed job bypass and enforced `QODANA_TOKEN` as a hard CI prerequisite.
- Fixed:
  - Stabilized fail-closed `tar.gz` handling (consistent behavior for GZip mismatch/nested cases).
- Docs/CI/Tooling:
  - Updated version convergence to `5.2.1` (`RepoVersion`, `Version`, `PackageVersion`, version history DE/EN).

## [5.2.0]
- Added:
  - Completed in-code documentation for TFM provider methods (`HashPrimitivesProvider` for `netstandard2.0` and `net8.0+`).
  - Added legacy Office refinement (`LegacyOfficeBinaryRefiner`) for OLE2 documents with fail-closed marker logic.
  - Added extended E2E matrix tests for wrong extensions, corrupted payloads, and Office/OpenDocument variants.
- Changed:
  - Promoted the release line from `v5.2.0-rc.6` to the stable tag `v5.2.0`.
  - Aligned public XML docs with Policy 045 by removing invalid `<exception>` tags from fail-closed APIs.
  - Harmonized German log/doc strings to use proper umlauts.
  - Consolidated Office/OpenOffice alias resolution in `FileTypeRegistry` (`doc/docx/odt`, `xls/xlsx/ods`, `ppt/pptx/odp`).
  - Archive extraction now accepts only real, extractable archives (`Zip`); Office containers are no longer treated as extractable archives.
  - `TryValidateArchive` now verifies detected container kind before archive safety checks.
  - Hardened Gate 4 (`tools/ci/release/gate4_verify_postpublish.sh`) for pre-release tags:
    - longer retry window,
    - `registration` decoupled by default for `vX.Y.Z-<label>`,
    - still fail-closed on `flatcontainer`.
  - Updated release retention (`tools/ci/release/retention_apply.sh`) so RCs are cleaned up once the corresponding stable tag `vX.Y.Z` exists (for example `v5.2.0-rc.6` -> `v5.2.0`), while an RC without a corresponding stable tag is kept.
- Fixed:
  - Resolved release blocker where pre-release publishing failed due delayed registration visibility despite successful push.
- Docs/CI/Tooling:
  - Updated NuGet trusted-publishing docs (DE/EN) and root README for the adjusted Gate 4 behavior.
  - Added commit reference for version 5.2.0 in version history (DE/EN).
  - Hardened retention workflow to secret-based NuGet unlist (`NUGET_API_KEY`); OIDC remains scoped to `release.yml`.

## [5.1.4]
- Changed:
  - Cluster 7C: consolidated and semantically unified ReasonCodes/exceptions (DetectDetailed).
  - Archive infrastructure: removed redundant `System.IO.*` qualifiers (Imports + unqualified usage).
- Fixed:
  - Qodana findings: removed `ConvertToConstant.Local` and `UseUtf8StringLiteral` in tests.
- Docs/CI/Tooling:
  - Decoupled the code-scanning zero gate for Qodana fix PRs (PRs with label `area:qodana` check the PR ref instead of `main`).

## [5.1.3]
- Added:
  - Mandatory PR template in German with checklists, review-fix tracking, and a DoD matrix.
  - Governance policy for branch/PR naming (`docs/governance/107_POLICY_BRANCH_PR_NAMING_DE.MD`).
  - FsCheck-based fuzzing smoke test as an auditable fuzzing signal test.
- Changed:
  - `preflight` now enforces fail-closed:
    - PR governance check (branch/title/PR body),
    - `security/code-scanning/tools` must report 0 open alerts.
  - Scorecard workflow uses `SECURITY_CLAIMS_TOKEN` as a PAT-based token source for robust governance evaluation.
  - Agent rules (`AGENTS.md`) extended with binding iterative processing and merge gates.
- Fixed:
  - Version convergence updated to `5.1.3` (`RepoVersion`, `Version`, `PackageVersion`, version history).
- Docs/CI/Tooling:
  - Governance index extended with branch/PR naming policy.

## [5.1.2]
- Changed:
  - Optimized Gate4 polling duration for release verification.
- Fixed:
  - Stabilized release/SVT path for `release_tag` deterministically.

## [5.1.1]
- Added:
  - Dependabot protections for secret-required workflows (`qodana`, `security-claims-evidence`) in Dependabot PR context.
- Changed:
  - Switched Dependabot to security-only for planned version bumps (`open-pull-requests-limit: 0` per ecosystem).
  - Hardened `security-claims-evidence` with explicit fail-closed secret assert (`SECURITY_CLAIMS_TOKEN`).
- Fixed:
  - Eliminated secret-caused CI failures on Dependabot PRs for `qodana` / `security-claims-evidence`.
- Docs/CI/Tooling:
  - Sorted version history semver-desc and corrected outdated release references.

## [5.0.0]
- Added:
  - Optional HMAC-SHA256 digests for physical and logical evidence hashes (`HmacPhysicalSha256`, `HmacLogicalSha256`) including ENV key resolver (`FILECLASSIFIER_HMAC_KEY_B64`).
- Changed:
  - Breaking rename of the public hashing facade from `DeterministicHashing` to `EvidenceHashing`.
  - Breaking rename of the public hashing models to `HashEvidence`, `HashDigestSet`, `HashOptions`, `HashRoundTripReport`, `HashSourceType`.
  - Consolidated hashing tests into a single file `tests/FileTypeDetectionLib.Tests/Unit/HashingEvidenceTests.cs`.
- Fixed:
  - Deterministically unified xxHash3 formatting and options (lowercase, `x16`, empty fast hashes when the option is disabled).
- Docs/CI/Tooling:
  - Added migration docs for hashing renames and HMAC key setup.

## [4.4.0]
- Added:
  - New governance policies for unified naming and SVT versioning (`CI-NAMING-001`, `CI-VERSION-001`).
- Changed:
  - Unified the canonical package/assembly/namespace name to `Tomtastisch.FileClassifier`.
  - Switched release publish to NuGet trusted publishing (OIDC via `NuGet/login@v1`), hardened fail-closed.
- Fixed:
  - Made PR labeling robust against paginated GitHub file lists (deterministic JSON generation).
  - Fixed release workflow YAML syntax for fail-closed assert.
- Docs/CI/Tooling:
  - Extended CI with dedicated checks `naming-snt` and `versioning-svt` including artifact evidence.
  - Added migration docs for the legacy NuGet ID (`Tomtastisch.FileTypeDetection`).

## [4.2.1]
- Added:
- Changed:
  - Hardened quality gates to explicit `Release` build/test/coverage.
  - Versioning guard now enforces a history entry for the active version.
- Fixed:
  - Stabilized Reqnroll/xUnit release regeneration against stale `obj/Release` artifacts.
- Docs/CI/Tooling:
  - Backfilled version history `docs/versioning/102_HISTORY_VERSIONS.MD` for missing `4.x` entries.
  - Extended versioning policy with a required evidence entry in the version line.

## [4.1.2]
- Added:
- Changed:
  - Migrated test toolchain to xUnit v3 + `Reqnroll.xunit.v3`.
  - Stabilized BDD discovery/runner execution for xUnit v3 (scenario parity preserved).
- Fixed:
- Docs/CI/Tooling:
  - Refactored CI into separate, auditable jobs (preflight, build, security, tests+coverage, summary).
  - BDD-readable test run produces TRX + readable output + coverage + gate in a single run.
  - Added CI evidence documentation under `docs/ci/101_PIPELINE_CI.MD`.
  - Maintained migration DoD and progress tracking in the PR description.

## [3.0.0]
- Breaking: rename `FileTypeDetectorOptions` -> `FileTypeProjectOptions`
- Breaking: rename `FileTypeSecurityBaseline` -> `FileTypeProjectBaseline`

## [2.0.0]
- Breaking: rename `ZipProcessing` -> `ArchiveProcessing`

## [1.0.0]
- Initial implementation
