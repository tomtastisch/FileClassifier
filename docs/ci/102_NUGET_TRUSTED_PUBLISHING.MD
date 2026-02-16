<!-- LANG_SWITCH:BEGIN -->
[DE](002_NUGET_TRUSTED_PUBLISHING.MD) | [EN](102_NUGET_TRUSTED_PUBLISHING.MD)
<!-- LANG_SWITCH:END -->

# NuGet Trusted Publishing (OIDC)

## 1. Purpose
Release publishing to nuget.org uses OIDC trusted publishing in `.github/workflows/release.yml` via `NuGet/login@v1`.

## 2. Binding
The active trusted-publishing policy is bound to the following identity:
- Repository: `tomtastisch/FileClassifier`
- Workflow: `release.yml`
- Environment: unused

## 3. Secret Usage
- Publishing does **not** use `secrets.NUGET_API_KEY`.
- `secrets.NUGET_API_KEY` remains only as a fallback for legacy operations (e.g. delete/unlist/deprecate) outside the OIDC publishing path.

## 4. Post-Publish SVT Gate (Propagation Delay)
- After a successful `dotnet nuget push`, NuGet indexing (search/registration/flatcontainer) can become visible with a delay.
- Gate 4 in the release workflow remains **fail-closed** for publish-critical endpoints:
  - `REQUIRE_FLATCONTAINER=1`
  - `REQUIRE_REGISTRATION=1`
  - `REQUIRE_SEARCH=0` (search is decoupled from the blocking gate).
- Default wait window for Gate 4:
  - `SVT_POSTPUBLISH_RETRY_SCHEDULE_SECONDS=2,3,5,8,13,21,34,55,89,89,89`
  - `SVT_POSTPUBLISH_RETRY_COUNT=11` (derived dynamically from schedule length unless explicitly set)
  - `SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS=10`
  - deterministically corresponds to a retry sleep budget of up to 408s (6m48s), plus HTTP timeout portions per attempt.
- For incident diagnostics, the window can be increased via these variables without changing workflow job names or required contexts.

## 5. Decoupled Online Convergence (Async)
- Workflow: `.github/workflows/nuget-online-convergence.yml`
- Trigger: `workflow_run` after a successful `Release Publish`.
- The async workflow still checks **all** endpoints as hard convergence conditions:
  - `REQUIRE_SEARCH=1`
  - `REQUIRE_REGISTRATION=1`
  - `REQUIRE_FLATCONTAINER=1`
- If convergence is missing, an incident issue is created/updated and the `nuget-online-convergence` log artifact is linked.
