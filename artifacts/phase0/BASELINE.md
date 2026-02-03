# A0) BASELINE.md

## Scope
- Phase 0 only (read-only diagnosis + artifact generation).
- Working directory: `/Users/tomwerner/RiderProjects/FileClassifier`.

## Repository Baseline
- Branch: `main` (evidence: `git rev-parse --abbrev-ref HEAD` in `artifacts/phase0/COMMANDS_RUN.md`).
- HEAD commit: `a997a91bb6f623c0535eca8cb83d0dc47c66afa2` (evidence: same command output).
- Dirty state: **32** entries in status (`git status --short | wc -l`), includes modified + renamed + untracked files (evidence: `artifacts/phase0/COMMANDS_RUN.md`).

## Platform / Toolchain
- OS: Darwin arm64 (Apple Silicon) (evidence: `uname -a` output in `artifacts/phase0/COMMANDS_RUN.md`).
- .NET SDK: `10.0.102`, runtimes `Microsoft.NETCore.App 10.0.2`, `Microsoft.AspNetCore.App 10.0.2` (evidence: `dotnet --version`, `dotnet --info`; see `artifacts/phase0/COMMANDS_RUN.md`).
- `global.json`: not present (evidence: `dotnet --info` section "global.json file: Not found").

## Structure Inventory (Top-level)
Top-level entries (deterministic sort):
- `.git`, `.gitignore`, `.idea`, `FileClassifier.sln`, `artifacts`, `portable`, `src`, `tests`, `tools`.
- Evidence: `find . -maxdepth 1 -mindepth 1 -print | sort` in `artifacts/phase0/COMMANDS_RUN.md`.

Detailed file inventory is in `artifacts/phase0/INVENTORY.tsv` (sorted, 4-column TSV).

## Build / Test / Run / Lint / Format Entry Points
### Build/Test/Run (documented/explicit)
- Solution/project graph:
  - `FileClassifier.sln` references:
    - `src/FileTypeDetectionLib/FileTypeDetectionLib.vbproj` (`FileClassifier.sln:5`)
    - `src/FileClassifier.App/FileClassifier.App.csproj` (`FileClassifier.sln:7`)
    - `tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj` (`FileClassifier.sln:9`)
- App runtime target:
  - `src/FileClassifier.App/FileClassifier.App.csproj:8-12` (`OutputType=Exe`, `TargetFramework=net10.0`).
- Test runner path:
  - `tools/test-bdd-readable.sh:5-7` -> `dotnet test ... --logger "console;verbosity=detailed"`.
  - `tests/FileTypeDetectionLib.Tests/README.md:11-20` documents BDD and full-suite test commands.
- Portable verification path:
  - `tools/check-portable-filetypedetection.sh:21-29` usage and `:110-117` restore/build/run sequence.

### Lint / Format
- No explicit lint config detected (`.editorconfig`, `stylecop`, eslint/prettier, etc. not found in scanned files).
- No dedicated format script found in repo scripts.
- Evidence: inventory/pattern scan in `artifacts/phase0/COMMANDS_RUN.md` (search output contains no such files).

## Dependency Overview
### Dependency manager(s)
- Primary manager: NuGet via SDK-style .NET projects.

### Runtime dependencies (pinned)
- `src/FileTypeDetectionLib/FileTypeDetectionLib.vbproj:9-11`
  - `DocumentFormat.OpenXml` `3.4.1`
  - `Mime` `3.8.0`
  - `Microsoft.IO.RecyclableMemoryStream` `3.0.1`
- Framework reference:
  - `src/FileTypeDetectionLib/FileTypeDetectionLib.vbproj:15` -> `Microsoft.AspNetCore.App`.

### Test dependencies (pinned)
- `tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj:11-16`
  - `coverlet.collector` `6.0.4`
  - `Microsoft.NET.Test.Sdk` `17.14.1`
  - `Reqnroll.Tools.MsBuild.Generation` `3.3.3`
  - `Reqnroll.xUnit` `3.3.3`
  - `xunit` `2.9.3`
  - `xunit.runner.visualstudio` `3.1.4`

### Locking / pinning status
- Package versions are pinned in project files (exact `Version=` values above).
- No lockfiles detected (`packages.lock.json`, `package-lock.json`, `pnpm-lock.yaml`, `uv.lock`, etc.) in repository scan.
- Evidence: lockfile pattern scan command in `artifacts/phase0/COMMANDS_RUN.md` produced no matches.

## Docs / Policy / CI inventory (observed)
- Root documentation files:
  - `src/FileTypeDetectionLib/README.md`
  - `tests/FileTypeDetectionLib.Tests/README.md`
  - `portable/FileTypeDetection/README.md`
- Policy/automation scripts:
  - `tools/sync-doc-conventions.sh`
  - `tools/sync-portable-filetypedetection.sh`
  - `tools/check-portable-filetypedetection.sh`
  - `tools/test-bdd-readable.sh`
- CI folder markers (`.github`, `.gitlab`, `.azure-pipelines`) not found to depth 3.

