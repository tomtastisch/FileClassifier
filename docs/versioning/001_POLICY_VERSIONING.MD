<!-- LANG_SWITCH:BEGIN -->
[DE](001_POLICY_VERSIONING.MD) | [EN](101_POLICY_VERSIONING.MD)
<!-- LANG_SWITCH:END -->

# Versionierungs- und Release-Policy (SSOT)

## 1. Zweck
Diese Policy definiert verbindliche SemVer-Regeln (`MAJOR.MINOR.PATCH`) und die deterministische CI-Erzwingung.

## 2. Geltungsbereich
- Release-Tags `vX.Y.Z` (optional `-prerelease`) als einzige SSOT-Quelle fuer Paketversionen.
- PR-Versioning-Guard und Auto-Labeling in CI.

## 3. Begriffe
- SSOT: Single Source of Truth fuer Versioninformation.
- Breaking Change: Aenderung, die bestehende Integrationen ohne Anpassung brechen kann.

## 4. SemVer-Regeln (verbindlich)
- `MAJOR`
  - oeffentliche API entfernt/umbenannt
  - inkompatibles Standardverhalten
- `MINOR`
  - neue abwaertskompatible Features oder API-Erweiterungen
- `PATCH`
  - Fixes, Refactorings ohne Verhaltensbruch, Tests, Doku, CI, Tooling
- `NONE`
  - reine Metadatenaenderungen ohne Versionsauswirkung
- Pre-Release-Tags sind erlaubt:
  - `vX.Y.Z-<label>` wird exakt als NuGet-Version `X.Y.Z-<label>` uebernommen.

## 5. Entscheidungs-Matrix
| Aenderungstyp | Beispiel | Bump |
|---|---|---|
| Public API rename/remove | `ZipProcessing` -> `ArchiveProcessing` | MAJOR |
| Breaking Defaults/Policy | stricteres Verhalten ohne Opt-in | MAJOR |
| Kompatibles Feature | neuer Datentyp/Flow | MINOR |
| Fix/Refactor | Stabilisierung, Cleanup | PATCH |
| Doku/Test/CI/Tooling | Workflow-, Skript-, Test-, Doku-Update | PATCH |
| Metadaten-only | mechanische Aenderung ohne Laufzeitwirkung | NONE |

## 6. CI-Guard (Pflicht)
- `tools/versioning/check-version-policy.sh` erzwingt fail-closed:
  - In PR/CI: keine statischen Versionfelder in `Directory.Build.props`/`Directory.Build.targets`/`*.csproj`/`*.vbproj`.
  - In Release (Tag): Tag-Format-Validierung und `TagVersion == nupkg-PackageVersion`.
- Publish ist nur bei `refs/tags/v*` erlaubt.
- Target-Feed fuer Online-Installierbarkeit: NuGet.org.
- Release-Packaging setzt die Version ausschliesslich via:
  - `dotnet pack ... -p:PackageVersion=$VERSION -p:Version=$VERSION`
- Assembly-/File-Version werden deterministisch aus dem Tag-Kern abgeleitet:
  - `AssemblyVersion = MAJOR.MINOR.0.0`
  - `FileVersion = MAJOR.MINOR.PATCH.0`
- `tools/versioning/check-versioning.sh` bleibt fuer Deterministik der `required`-Klassifikation im PR-Labeling.

## 7. Deterministisches Auto-Labeling (Pflicht)
Caps pro PR-Lauf:
- genau ein `version:*`
- genau ein Primary-Label
- maximal ein `impl:*`
- maximal zwei `area:*`

Alle veralteten Auto-Labels werden vor dem Anwenden neuer Labels entfernt.

## 8. Label-Registry
### 8.1 Version-Labels
- `version:major`
- `version:minor`
- `version:patch`
- `version:none`

### 8.2 Primary-Labels
- `breaking`, `feature`, `fix`, `refactor`, `ci`, `test`, `docs`, `tooling`, `chore`

### 8.3 Implementation-Labels
- `impl:quality`, `impl:security`, `impl:docs`, `impl:config`

### 8.4 Area-Labels
- `area:pipeline`, `area:qodana`, `area:archive`, `area:hashing`, `area:detection`,
- `area:materializer`, `area:versioning`, `area:tests`, `area:docs`, `area:tooling`

Kanonische Quelle:
- `tools/versioning/labels.json`

## 9. Prioritaetsregel
Primary-Prioritaet ist fix:
`breaking > feature > fix > refactor > ci > test > docs > tooling > chore`

## 10. Zugehoerige SSOT-Dokumente
- `docs/versioning/004_POLICY_LABELING.MD`
- `docs/ci/001_PIPELINE_CI.MD`
- `docs/governance/002_POLICY_LABELING.MD`
- `docs/governance/004_POLICY_DOCUMENTATION.MD`

## 11. Qodana
Qodana ist zusaetzliche Static-Analysis und ersetzt keine CI-Quality-Gates fuer Build/Test/Coverage/Versioning.

## 12. Dependency-Upgrade-Policy (Canary)
- Patch-/Minor-Upgrades fuer risikoreiche Dependencies werden ueber den Canary-Workflow vorab verifiziert:
  - `.github/workflows/dependency-canary.yml`
  - `tools/ci/bin/dependency-canary.sh`
  - `tools/ci/policies/data/dependency_canary.json`
- Verbindliche Canary-Nachweise:
  - fokussierte dependency-spezifische Fail-closed-/Guard-Tests erfolgreich
  - Qodana-Lauf plus `run.sh qodana` Contract-Validierung erfolgreich
- Major-Upgrades (`X+1.0.0`) sind fuer alle Canary-Dependencies nicht automatisch:
  - nur via dediziertem PR mit API-Impact-Review und expliziter SemVer-Entscheidung

## RoC-Bezug
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
- [Shell-Safety-Regeln](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/shell_safety.yaml)
