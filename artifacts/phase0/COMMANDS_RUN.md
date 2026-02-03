# A2) COMMANDS_RUN.md

All commands were executed from `/Users/tomwerner/RiderProjects/FileClassifier`.

## Environment / SCM

### 1) Platform stamp
```bash
LC_ALL=C pwd && uname -a && date -u +"%Y-%m-%dT%H:%M:%SZ"
```
Output:
```text
/Users/tomwerner/RiderProjects/FileClassifier
Darwin Toms-MacBook-Pro.local 25.3.0 ... arm64
2026-02-03T17:12:39Z
```

### 2) Git baseline
```bash
LC_ALL=C git status --porcelain=v1 -b
LC_ALL=C git rev-parse --abbrev-ref HEAD
LC_ALL=C git rev-parse HEAD
```
Output (excerpt):
```text
## main
 M .gitignore
 ...
?? tools/test-bdd-readable.sh
main
a997a91bb6f623c0535eca8cb83d0dc47c66afa2
```

### 3) Dirty-state count
```bash
LC_ALL=C git status --short | wc -l
LC_ALL=C git status --short | sed -n '1,200p'
```
Output:
```text
      32
 M .gitignore
 RM portable/FileTypeDetection/Abstractions/index.md -> portable/FileTypeDetection/Abstractions/INDEX.md
 ...
?? tools/test-bdd-readable.sh
```

## Structure / Inventory Commands

### 4) Top-level listing
```bash
LC_ALL=C ls -la
LC_ALL=C find . -maxdepth 1 -mindepth 1 -print | LC_ALL=C sort
```
Output (top-level):
```text
./.git
./.gitignore
./.idea
./FileClassifier.sln
./artifacts
./portable
./src
./tests
./tools
```

### 5) Full non-generated file listing (for inventory source)
```bash
LC_ALL=C find . -type f \( -path './.git/*' -o -path '*/bin/*' -o -path '*/obj/*' \) -prune -o -type f -print | LC_ALL=C sort
```
Output: see command output captured in session; generated artifact uses same source: `artifacts/phase0/INVENTORY.tsv`.

### 6) Inventory artifact generation
```bash
python3 <script>  # wrote artifacts/phase0/INVENTORY.tsv
```
Output:
```text
wrote artifacts/phase0/INVENTORY.tsv
```

## Build/Test/Dependency Evidence Commands

### 7) Project and solution manifests with line numbers
```bash
nl -ba src/FileTypeDetectionLib/FileTypeDetectionLib.vbproj
nl -ba tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj
nl -ba src/FileClassifier.App/FileClassifier.App.csproj
nl -ba FileClassifier.sln | sed -n '1,220p'
```
Output highlights:
```text
FileTypeDetectionLib.vbproj: TargetFramework net10.0; PackageReference versions pinned.
FileTypeDetectionLib.Tests.csproj: test dependencies pinned.
FileClassifier.App.csproj: OutputType Exe; TargetFramework net10.0.
FileClassifier.sln: 3 projects declared.
```

### 8) Tooling scripts with line numbers
```bash
nl -ba tools/check-portable-filetypedetection.sh | sed -n '1,260p'
nl -ba tools/test-bdd-readable.sh
nl -ba tools/sync-portable-filetypedetection.sh | sed -n '1,260p'
nl -ba tools/sync-doc-conventions.sh | sed -n '1,260p'
```
Output highlights:
```text
tools/check-portable-filetypedetection.sh: restore/build/run portable smoke workflow.
tools/test-bdd-readable.sh: dotnet test with detailed console logger.
```

### 9) Documentation line-number snapshots
```bash
nl -ba src/FileTypeDetectionLib/README.md | sed -n '1,220p'
nl -ba tests/FileTypeDetectionLib.Tests/README.md | sed -n '1,260p'
nl -ba portable/FileTypeDetection/README.md | sed -n '1,260p'
```
Output highlights:
```text
src README: API table and navigation.
tests README: BDD-readable command and full-test command.
portable README: setup, API table, flow charts, maintenance commands.
```

### 10) Lockfile / CI / policy pattern checks
```bash
LC_ALL=C find . -type f \( -path './.git/*' -o -path '*/bin/*' -o -path '*/obj/*' \) -prune -o -type f -print | LC_ALL=C sort | rg -N "lock|global.json|nuget.config|Directory.Build|\.github|\.editorconfig|Dockerfile|compose|Makefile|justfile|package-lock|yarn.lock|pnpm-lock|poetry.lock|Pipfile.lock|uv.lock" || true
LC_ALL=C find . -maxdepth 3 -type d -name '.github' -o -name '.gitlab' -o -name '.azure-pipelines' | LC_ALL=C sort || true
```
Output:
```text
# (no matches)
# (no matches)
```

### 11) .NET toolchain baseline
```bash
dotnet --version
dotnet --info | sed -n '1,80p'
```
Output (excerpt):
```text
10.0.102
.NET SDK 10.0.102
RID: osx-arm64
global.json file: Not found
```

