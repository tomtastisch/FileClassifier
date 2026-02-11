# NuGet Trusted Publishing (OIDC)

## 1. Zweck
Release-Publish auf nuget.org erfolgt per OIDC Trusted Publishing in `/.github/workflows/release.yml` via `NuGet/login@v1`.

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
  - `REQUIRE_REGISTRATION=1`
  - `REQUIRE_SEARCH=0` (Search ist aus dem blocking Gate entkoppelt).
- Default-Wartefenster für Gate 4:
  - `SVT_POSTPUBLISH_RETRY_COUNT=59`
  - `SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS=10`
  - entspricht deterministisch bis zu 10 Minuten Wartezeit.
- Bei Incident-Diagnose kann das Fenster über die beiden Variablen erhöht werden, ohne Workflow-Jobnamen oder Required Contexts zu ändern.

## 5. Entkoppelte Online-Konvergenz (Async)
- Workflow: `/.github/workflows/nuget-online-convergence.yml`
- Trigger: `workflow_run` nach erfolgreichem `Release Publish`.
- Der Async-Workflow prüft weiterhin **alle** Endpunkte als harte Konvergenzbedingungen:
  - `REQUIRE_SEARCH=1`
  - `REQUIRE_REGISTRATION=1`
  - `REQUIRE_FLATCONTAINER=1`
- Bei fehlender Konvergenz wird ein Incident-Issue erstellt/ergänzt und das Log-Artefakt `nuget-online-convergence` verlinkt.
