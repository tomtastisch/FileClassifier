<!-- LANG_SWITCH:BEGIN -->
[DE](005_POLICY_NAMING.MD) | [EN](105_POLICY_NAMING.MD)
<!-- LANG_SWITCH:END -->

# Naming Policy (SNT Level 3)

## 1. Ziel und Geltungsbereich
Diese Policy definiert `Single Name Truth (SNT)` auf Level 3.
Sie gilt fuer:
- GitHub-Repository-Name
- NuGet `PackageId`
- `PackageProjectUrl` / `RepositoryUrl`
- Installations-Snippets in `README.md` und relevanten `docs/*`
- CI-/Release-Artefakte und Verifikationsskripte

## 2. Kanonisches SSOT-Tupel
Die einzige normative Quelle ist:
- `tools/ci/policies/data/naming.json`

Kanonische Felder (Schema `tools/ci/policies/data/naming.json`):
- `canonical_name`
- `product_name`
- `package_id`
- `assembly_name`
- `root_namespace`
- `repo_identity`
- `repo_slug`
- `repo_display_name`
- `repository_url`
- `deprecated_package_ids`
- `namespace_decision`
- `docs_expected_links`

Aenderungen am Namen erfolgen ausschliesslich durch Update dieser SSOT-Datei plus begleitende Migration.

## 3. Was konsistent sein MUSS
Harte Gleichheits-/Konsistenzregel (SNT strict identity):
- `canonical_name == package_id == assembly_name == root_namespace`
- `repo_slug` muss zum `origin`-Remote-Slug passen
- Eintraege in `deprecated_package_ids` duerfen nicht als kanonische Paketreferenz verwendet werden

Folgende Stellen MUESSEN dem SSOT entsprechen:
1. `src/FileTypeDetection/FileTypeDetectionLib.vbproj`
   - `PackageId == package_id`
   - `RepositoryUrl == repository_url`
   - `PackageProjectUrl == repository_url`
   - `AssemblyName == assembly_name`
   - `RootNamespace == root_namespace`
   - Quell-Namespace in `src/FileTypeDetection/**/*.vb` ist einheitlich unter `root_namespace`
2. `README.md` und definierte Nutzungsdokumente
   - Installationsbeispiele verwenden nur `package_id`
   - IDs aus `deprecated_package_ids` duerfen dort nicht als Install-Snippet auftauchen
3. CI/Release
   - `tools/ci/bin/run.sh` erzwingt in `run_pack` exakt `package_id`
   - `.github/workflows/release.yml` laeuft einen Naming-Gate vor Pack/Publish
4. Paketverbraucher im Repo
   - `samples/PortableConsumer/PortableConsumer.csproj`
   - `tests/PackageBacked.Tests/PackageBacked.Tests.csproj`
   - beide referenzieren nur `package_id`

## 4. Enforcement (Fail-Closed)
Deterministische Pruefung:
- `tools/ci/check-naming-snt.sh`
  - liest SSOT
  - prueft Projektmetadaten, Doku-Referenzen und Workflow/CI-Stellen
  - schreibt maschinenlesbar: `artifacts/ci/<check_id>/naming-snt-summary.json`

RaC-Regel (PolicyRunner):
- `tools/ci/policies/rules/naming_snt.yaml`
- `policy_type: naming_snt`
- Bei jeder Abweichung: `fail` (kein Soft-Bypass)
- Evidence muss enthalten: Datei + exakte Trefferposition (`file:line` oder eindeutiger Pfad)

## 5. NuGet-Migration (Level 3 Betrieb)
Zielbild auf nuget.org:
- Genau EIN Paket ist discoverable/recommended: `package_id`
- Legacy-Pakete aus `deprecated_package_ids` bleiben technisch existent, aber:
  - fuer alle Versionen unlisted
  - deprecated mit Verweis auf `package_id`
  - in Dokumentation nicht mehr empfohlen

Operative Schritte (Maintainer):
1. Neues Paket releasen (`package_id`).
2. Legacy-Migration vorbereiten/ausfuehren:
   - Dry-Run: `bash tools/ci/nuget-migrate-legacy-package.sh`
   - Operativ: `APPLY=1 NUGET_API_KEY=<key> bash tools/ci/nuget-migrate-legacy-package.sh`
3. Altes Paket unlisten (`nuget delete` auf nuget.org-Semantik = unlist).
4. Altes Paket deprecaten und Alternativpaket setzen.
5. Dokumentation auf neues Paket fixieren.
6. CI-Gates (`naming-snt`, `pack`, `summary`) muessen gruen sein.

Hinweis:
- Falls lokale NuGet-CLI kein `deprecate` unterstuetzt, erfolgt Deprecation im nuget.org UI (Manage Package -> Deprecation).
- Das Migrationsskript bricht in `APPLY=1` fail-closed ab, wenn Deprecation nicht automatisiert werden kann.

## 6. Namespace-Entscheidung
Die Namespace-Strategie ist explizit im SSOT hinterlegt (`namespace_decision`).
Aktueller Beschluss laut SSOT:
- `mode: rename`
- `current_root_namespace: FileTypeDetection`
- `target_root_namespace: Tomtastisch.FileClassifier`

Diese Umstellung ist als Breaking Change (major) zu behandeln und wird durch Migrationsdokumentation begleitet.

## 7. Nicht-Ziele
- Keine Hard-Delete-Annahme fuer nuget.org.
- Keine parallelen "empfohlenen" Paketnamen in Doku oder CI.
- Keine konfigurationslosen Sonderfaelle ausserhalb SSOT.

## 8. Post-Merge Release-Kontrolle
Nach Merge/Tag-Release gilt verpflichtend:
1. Release-Workflow fuer den Tag laufen lassen.
2. Artefakt `release-naming-snt-summary` herunterladen.
3. `naming-snt-summary.json` muss `\"status\": \"pass\"` enthalten.
4. Bei Abweichung ist der Release als nicht-konform zu behandeln.

## RoC-Bezug
- Naming-SNT-Regel: `tools/ci/policies/rules/naming_snt.yaml`
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
