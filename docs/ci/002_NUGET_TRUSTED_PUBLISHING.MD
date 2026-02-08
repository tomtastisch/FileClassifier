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
