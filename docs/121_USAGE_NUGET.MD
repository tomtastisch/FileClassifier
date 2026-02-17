<!-- LANG_SWITCH:BEGIN -->
[DE](021_USAGE_NUGET.MD) | [EN](121_USAGE_NUGET.MD)
<!-- LANG_SWITCH:END -->

# NuGet Usage

## A) Purpose
`Tomtastisch.FileClassifier` provides the public library surface for file type detection, safe archive processing, byte materialization, and deterministic hashing evidence.

## B) Install (Consumer)
```bash
dotnet add package Tomtastisch.FileClassifier --version X.Y.Z
```

```xml
<ItemGroup>
  <PackageReference Include="Tomtastisch.FileClassifier" Version="X.Y.Z" />
</ItemGroup>
```

Version pinning:
- For releases, SVT applies: Git tag `vX.Y.Z` equals package version `X.Y.Z`.
- Consumers should pin exactly to the released version.

## C) Minimal Usage (Consumer)
```csharp
using Tomtastisch.FileClassifier;

var payload = "%PDF-1.7\nsample\n"u8.ToArray();
var detector = new FileTypeDetector();
var detected = detector.Detect(payload);

if (detected.Kind == FileKind.Pdf)
{
    var evidence = EvidenceHashing.HashBytes(payload, "sample.pdf");
    _ = FileMaterializer.Persist(payload, "out/sample.bin", overwrite: true, secureExtract: false);
}
```

## D) Verification (Consumer)
Local package verification (package metadata vs expectation only):
```bash
EXPECTED_VERSION=X.Y.Z VERIFY_ONLINE=0 bash tools/ci/verify_nuget_release.sh
```

Online verification (search, registration, flatcontainer):
```bash
EXPECTED_VERSION=X.Y.Z bash tools/ci/verify_nuget_release.sh
```

Relevant inputs:
- `EXPECTED_VERSION`: expected target (`X.Y.Z`).
- `NUPKG_PATH`: explicit package path (optional).
- `NUPKG_DIR`: search directory (default `artifacts/nuget` if `NUPKG_PATH` is not set).
- `VERIFY_ONLINE=0`: deterministically skips network checks.
- `RETRY_COUNT`: number of additional retries after the first attempt for online checks (default `6`).
- `RETRY_SLEEP_SECONDS`: sleep time between retries in seconds (default `10`).
- `REQUIRE_SEARCH`: enable/disable search as required check (`1`/`0`, default `1`).
- `REQUIRE_REGISTRATION`: enable/disable registration as required check (`1`/`0`, default `1`).
- `REQUIRE_FLATCONTAINER`: enable/disable flatcontainer as required check (`1`/`0`, default `1`).

Release Gate 4 (post-publish) uses its own defaults via the wrapper:
- `SVT_POSTPUBLISH_RETRY_SCHEDULE_SECONDS` (Default `2,3,5,8,13,21,34,55,89,89,89`)
- `SVT_POSTPUBLISH_RETRY_COUNT` (default: schedule element count, currently `11`; optionally overrideable)
- `SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS` (Default `10`)
- Blocking scope in release:
  - `REQUIRE_SEARCH=0`
  - `REQUIRE_REGISTRATION=1`
  - `REQUIRE_FLATCONTAINER=1`

Async full convergence:
- Workflow `.github/workflows/nuget-online-convergence.yml` checks all three endpoints after a successful release with a longer retry window.

## E) Maintainer: Token Storage and Local Publish (Secure)
Keychain (prompt-based):
```bash
security add-generic-password -a "$USER" -s "NUGET_API_KEY" -U -w
```

Read (silent):
```bash
security find-generic-password -a "$USER" -s "NUGET_API_KEY" -w 2>/dev/null
```

Secure local publish flow:
```bash
EXPECTED_VERSION=X.Y.Z bash tools/ci/publish_nuget_local.sh
```

Notes:
- Never print the token or write it to logs.
- `dotnet nuget push` runs with `--skip-duplicate`.
- After push, the helper runs an SVT online verification.

## F) Troubleshooting
- A `404` on registration/flatcontainer can be caused by replication delay; the verifier retries deterministically.
- If versions differ (`expected != actual`), the gate fails before publish; fix the version source (tag/pack overrides).
- If `missing_version` appears right after publish, it is usually NuGet propagation. For incident checks:
  ```bash
  EXPECTED_VERSION=X.Y.Z VERIFY_ONLINE=1 RETRY_COUNT=60 RETRY_SLEEP_SECONDS=10 bash tools/ci/verify_nuget_release.sh
  ```
- Release Gate 4 intentionally does not block on search. Search convergence is checked separately in the async workflow and escalated as an incident on failure.

## G) Runbook: RC version not visible in Visual Studio
Symptom:
- NuGet Package Manager shows only stable versions (for example `Latest stable version`).

Deterministic cause:
- The pre-release filter is disabled in the Visual Studio view, or local metadata cache is stale.

Steps:
1. Enable `Include prerelease` in NuGet Package Manager.
2. Verify the source is [NuGet v3 index](https://api.nuget.org/v3/index.json).
3. Clear local cache:
   ```bash
   dotnet nuget locals all --clear
   ```
4. Restart Visual Studio and refresh package metadata.
5. If still not visible, pin the RC version explicitly:
   ```xml
   <ItemGroup>
     <PackageReference Include="Tomtastisch.FileClassifier" Version="X.Y.Z-rc.N" />
   </ItemGroup>
   ```

Verification:
- Search without pre-release flag returns stable versions only.
- Search with `prerelease=true&semVerLevel=2.0.0` returns RC versions.
