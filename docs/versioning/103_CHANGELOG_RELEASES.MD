<!-- LANG_SWITCH:BEGIN -->
[DE](003_CHANGELOG_RELEASES.MD) | [EN](103_CHANGELOG_RELEASES.MD)
<!-- LANG_SWITCH:END -->

# Changelog

All changes are documented here in technical terms. The release version itself is the Git tag `vX.Y.Z` (optional `-prerelease`) as SSOT.

## [Unreleased]
- Added:
  - Completed in-code documentation for TFM provider methods (`HashPrimitivesProvider` for `netstandard2.0` and `net8.0+`).
  - Added legacy Office refinement (`LegacyOfficeBinaryRefiner`) for OLE2 documents with fail-closed marker logic.
  - Added extended E2E matrix tests for wrong extensions, corrupted payloads, and Office/OpenDocument variants.
- Changed:
  - Aligned public XML docs with Policy 045 by removing invalid `<exception>` tags from fail-closed APIs.
  - Harmonized German log/doc strings to use proper umlauts.
  - Consolidated Office/OpenOffice alias resolution in `FileTypeRegistry` (`doc/docx/odt`, `xls/xlsx/ods`, `ppt/pptx/odp`).
  - Archive extraction now accepts only real, extractable archives (`Zip`); Office containers are no longer treated as extractable archives.
  - `TryValidateArchive` now verifies detected container kind before archive safety checks.
  - Hardened Gate 4 (`tools/ci/release/gate4_verify_postpublish.sh`) for pre-release tags:
    - longer retry window,
    - `registration` decoupled by default for `vX.Y.Z-<label>`,
    - still fail-closed on `flatcontainer`.
- Fixed:
  - Resolved release blocker where pre-release publishing failed due delayed registration visibility despite successful push.
- Docs/CI/Tooling:
  - Updated NuGet trusted-publishing docs (DE/EN) and root README for the adjusted Gate 4 behavior.
  - Added commit reference for version 5.2.0 in version history (DE/EN).

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
