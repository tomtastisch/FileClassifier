<!-- LANG_SWITCH:BEGIN -->
[DE](002_NUGET_TRUSTED_PUBLISHING.MD) | [EN](../../1_en/ci/002_NUGET_TRUSTED_PUBLISHING.MD)
<!-- LANG_SWITCH:END -->

# NuGet Trusted Publishing (OIDC)

## 1. Zweck
Release-Publish auf nuget.org erfolgt per OIDC Trusted Publishing in `.github/workflows/release.yml` via `NuGet/login@v1`.

## 2. Binding
Die aktive Trusted-Publishing-Policy ist an folgende Identität gebunden:
- Repository: `tomtastisch/FileClassifier`
- Workflow: `release.yml`
- Environment: ungenutzt

## 3. Secret-Verwendung
- Publish verwendet **nicht** `secrets.NUGET_API_KEY`.
- `secrets.NUGET_API_KEY` bleibt nur Fallback für Legacy-Operationen (z. B. delete/unlist/deprecate) außerhalb des OIDC-Publish-Pfads.

## 4. Post-Publish SVT Gate (Propagation-Delay)
- Nach erfolgreichem `dotnet nuget push` kann die NuGet-Indexierung (Search/Registration/Flatcontainer) zeitverzögert sichtbar sein.
- Gate 4 im Release-Workflow bleibt **fail-closed** für publish-kritische Endpunkte:
  - `REQUIRE_FLATCONTAINER=1`
  - `REQUIRE_REGISTRATION=1` für stabile Tags `vX.Y.Z`
  - `REQUIRE_REGISTRATION=0` für Pre-Release-Tags `vX.Y.Z-<label>` (per Default; via `SVT_POSTPUBLISH_REQUIRE_REGISTRATION=1` übersteuerbar)
  - `REQUIRE_SEARCH=0` (Search ist aus dem blocking Gate entkoppelt).
- Default-Wartefenster für Gate 4:
  - stabil: `SVT_POSTPUBLISH_RETRY_SCHEDULE_SECONDS=2,3,5,8,13,21,34,55,89,89,89`
  - pre-release: `SVT_POSTPUBLISH_RETRY_SCHEDULE_SECONDS=2,3,5,8,13,21,34,55,89,144,233,377`
  - `SVT_POSTPUBLISH_RETRY_COUNT` dynamisch aus Schedule-Länge, sofern nicht explizit gesetzt
  - `SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS=10`
  - entspricht deterministisch einem Retry-Sleep-Budget von bis zu 408s (stabil) bzw. 984s (pre-release), jeweils zuzüglich HTTP-Timeout-Anteilen pro Versuch.
- Bei Incident-Diagnose kann das Fenster über die beiden Variablen erhöht werden, ohne Workflow-Jobnamen oder Required Contexts zu ändern.

## 5. Entkoppelte Online-Konvergenz (Async)
- Zusätzlich läuft im Release-Workflow ein synchrones Blocker-Gate `enforce-online-convergence`, das nach Publish die Endpunkt-Konvergenz strikt erzwingt (auch für `workflow_dispatch`-Releases).
- Workflow: `.github/workflows/nuget-online-convergence.yml`
- Trigger: `workflow_run` nach erfolgreichem `Release Publish` (unabhängig davon, ob via Tag-Push oder `workflow_dispatch` ausgelöst).
- Der Async-Workflow prüft weiterhin **alle** Endpunkte als harte Konvergenzbedingungen:
  - `REQUIRE_SEARCH=1`
  - `REQUIRE_REGISTRATION=1`
  - `REQUIRE_FLATCONTAINER=1`
- Bei fehlender Konvergenz wird ein Incident-Issue erstellt/ergänzt und das Log-Artefakt `nuget-online-convergence` verlinkt.
