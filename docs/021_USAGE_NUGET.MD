# NuGet Usage

## A) Purpose
`Tomtastisch.FileClassifier` stellt die öffentliche Bibliotheksoberfläche für Dateityperkennung, sichere Archivverarbeitung, Byte-Materialisierung und deterministische Hashing-Nachweise bereit.

## B) Install (Consumer)
```bash
dotnet add package Tomtastisch.FileClassifier --version X.Y.Z
```

```xml
<ItemGroup>
  <PackageReference Include="Tomtastisch.FileClassifier" Version="X.Y.Z" />
</ItemGroup>
```

Version-Pinning:
- Für Releases gilt SVT: Git-Tag `vX.Y.Z` entspricht Paketversion `X.Y.Z`.
- Consumer sollten exakt auf die freigegebene Version pinnen.

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
Lokale Paketprüfung (nur Paketmetadata gegen Erwartung):
```bash
EXPECTED_VERSION=X.Y.Z VERIFY_ONLINE=0 bash tools/ci/verify_nuget_release.sh
```

Online-Prüfung (Search, Registration, Flatcontainer):
```bash
EXPECTED_VERSION=X.Y.Z bash tools/ci/verify_nuget_release.sh
```

Relevante Inputs:
- `EXPECTED_VERSION`: erwartetes Ziel (`X.Y.Z`).
- `NUPKG_PATH`: expliziter Paketpfad (optional).
- `NUPKG_DIR`: Suchverzeichnis (Default `artifacts/nuget`, falls `NUPKG_PATH` nicht gesetzt).
- `VERIFY_ONLINE=0`: überspringt Netzwerkchecks deterministisch.
- `RETRY_COUNT`: Anzahl zusätzlicher Wiederholungen nach dem Erstversuch für Online-Checks (Default `6`).
- `RETRY_SLEEP_SECONDS`: Wartezeit zwischen Retries in Sekunden (Default `10`).
- `REQUIRE_SEARCH`: aktiviert/deaktiviert Search als Pflichtcheck (`1`/`0`, Default `1`).
- `REQUIRE_REGISTRATION`: aktiviert/deaktiviert Registration als Pflichtcheck (`1`/`0`, Default `1`).
- `REQUIRE_FLATCONTAINER`: aktiviert/deaktiviert Flatcontainer als Pflichtcheck (`1`/`0`, Default `1`).

Release-Gate-4 (post-publish) nutzt eigene Defaults über den Wrapper:
- `SVT_POSTPUBLISH_RETRY_COUNT` (Default `59`)
- `SVT_POSTPUBLISH_RETRY_SLEEP_SECONDS` (Default `10`)
- Blocking-Scope im Release:
  - `REQUIRE_SEARCH=0`
  - `REQUIRE_REGISTRATION=1`
  - `REQUIRE_FLATCONTAINER=1`

Asynchrone Full-Convergence:
- Workflow `/.github/workflows/nuget-online-convergence.yml` prüft nach erfolgreichem Release weiterhin alle drei Endpunkte mit längerem Retry-Fenster.

## E) Maintainer: Token Storage and Local Publish (Secure)
Keychain (prompt-basiert):
```bash
security add-generic-password -a "$USER" -s "NUGET_API_KEY" -U -w
```

Abruf (silent):
```bash
security find-generic-password -a "$USER" -s "NUGET_API_KEY" -w 2>/dev/null
```

Sicherer lokaler Publish-Flow:
```bash
EXPECTED_VERSION=X.Y.Z bash tools/ci/publish_nuget_local.sh
```

Hinweise:
- Token nie ausgeben oder in Logs schreiben.
- `dotnet nuget push` läuft mit `--skip-duplicate`.
- Nach Push führt der Helper eine SVT-Onlineprüfung aus.

## F) Troubleshooting
- `404` bei Registration/Flatcontainer kann durch Replikationsverzögerung entstehen; der Verifier retried deterministisch.
- Bei Versionsabweichung (`expected != actual`) failt der Gate vor Publish; Version-Quelle (Tag/Pack-Overrides) korrigieren.
- Wenn direkt nach Publish `missing_version` erscheint, ist das meist NuGet-Propagation. Für Incident-Checks:
  ```bash
  EXPECTED_VERSION=X.Y.Z VERIFY_ONLINE=1 RETRY_COUNT=60 RETRY_SLEEP_SECONDS=10 bash tools/ci/verify_nuget_release.sh
  ```
- Release-Gate-4 prüft Search bewusst nicht blockierend. Search-Konvergenz wird separat im Async-Workflow geprüft und bei Fehlern als Incident eskaliert.
