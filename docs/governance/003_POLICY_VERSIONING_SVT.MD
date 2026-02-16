<!-- LANG_SWITCH:BEGIN -->
[DE](003_POLICY_VERSIONING_SVT.MD) | [EN](103_POLICY_VERSIONING_SVT.MD)
<!-- LANG_SWITCH:END -->

# Versionierungsrichtlinie (SVT)

## 1. Zweck
Diese Richtlinie erzwingt Single Version Truth (SVT) fuer die kanonische Paket-Release-Pipeline.

## 2. SSOT
Normative Quelle: `tools/ci/policies/data/versioning.json`.

## 3. SVT-Definition
Fuer das kanonische Paket `Tomtastisch.FileClassifier` MUESSEN die folgenden Werte exakt uebereinstimmen:
- `git tag vX.Y.Z`
- vbproj `<Version>X.Y.Z</Version>`
- vbproj `<PackageVersion>X.Y.Z</PackageVersion>`
- erzeugte `.nupkg`-Version `X.Y.Z`
- veroeffentlichte NuGet-Version `X.Y.Z` (Release-Job)

## 4. SHALL-Statements
- Release-Tags SHALL den Prefix `v` sowie einen semantischen Versionskern verwenden (`vMAJOR.MINOR.PATCH`).
- Der Release-Pack-Schritt SHALL `VERSION=${GITHUB_REF_NAME#v}` ableiten.
- `dotnet pack` SHALL `-p:Version=$VERSION -p:PackageVersion=$VERSION` setzen.
- CI SHALL bei Mehrdeutigkeit fail-closed abbrechen (z. B. mehrere passende Tags auf HEAD).
- Legacy-Paketversionen liegen ausserhalb des kanonischen SVT-Scopes und SHALL als veraltet/deprecated behandelt werden.

## 5. CI-Enforcement-Mapping
- Rule-ID: `CI-VERSION-001`.
- Regeldatei: `tools/ci/policies/rules/versioning_svt.yaml`.
- Checker: `tools/ci/check-versioning-svt.sh`.
- Report: `artifacts/versioning_report.json`.
