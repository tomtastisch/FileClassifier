# Versioning & Release Policy

## 1. Ziel
Diese Policy definiert eine **strikte SemVer-Strategie** (MAJOR.MINOR.PATCH), die
**rueckwirkend** auf die gesamte Commit-Historie angewandt wird und zukuenftig
**deterministisch** durch CI geprueft wird.

## 2. Begriffe
- **SSOT (Single Source of Truth):** `Directory.Build.props` ist die einzige Quelle
  fuer die aktuelle Version.
- **Breaking Change:** Aenderung, die bestehende Integrationen bricht
  (Public API/Behavior/Defaults).

## 3. SemVer-Regeln (verbindlich)
- **MAJOR**
  - Public API aendern/entfernen/umbenennen.
  - Default-Policy/Behavior so aendern, dass bisherige Aufrufe scheitern
    oder anderes Ergebnis liefern.
  - Beispiele: `ZipProcessing` -> `ArchiveProcessing`,
    `FileTypeDetectorOptions` -> `FileTypeProjectOptions`.
- **MINOR**
  - Neue Funktionalitaet, Datatypes, Archive-Formate oder API-Surface,
    **kompatibel** zu bestehenden Aufrufen.
- **PATCH**
  - Fixes, Refactoring ohne Funktionsaenderung, Tests, Doku, CI, Tools.

## 4. Meta-Aenderungen
- Einfuehrung von Versionierungs-Mechanik (`b62ac79`) ist **Meta** und fuehrt
  **nicht** zu einem SemVer-Bump.

## 5. Rueckwirkende Anwendung
Diese Policy wird **rueckwirkend** auf alle Commits angewandt.
Die aktuelle Version ergibt sich deterministisch aus der Historie.

## 6. Decision Matrix
| Aenderung | Beispiel | Bump |
|---|---|---|
| Public API rename/remove | `ZipProcessing` -> `ArchiveProcessing` | MAJOR |
| Default-Policy Breaking | strengere Defaults ohne Opt-In | MAJOR |
| Neue kompatible Features | neue Archive-Typen, neue Modelle | MINOR |
| Bugfix/Refactor | Stabilisierung, Cleanup | PATCH |
| Docs/Test/CI/Tooling | Readme, Tests, Workflows | PATCH |
| Meta/Versioning | `b62ac79` | NONE |

## 7. Changelog-Konventionen
- `docs/versioning/CHANGELOG.md` hat eine **Unreleased** Sektion.
- Wenn eine Aenderung **breaking** ist, muss dort eine Zeile mit `BREAKING:`
  stehen, damit CI den Major-Bump erzwingt.

## 8. CI-Guard (Pflicht)
- `tools/versioning/check-versioning.sh` klassifiziert Aenderungen und prueft,
  ob die SSOT-Version korrekt gebumpt wurde.
- Der Job `versioning-guard` muss vor Build/Test erfolgreich sein.

## 8.1 Labels (verbindlich)
### SemVer-Labels
- `version:major` => Breaking-Change, MAJOR-Bump erforderlich
- `version:minor` => kompatible Erweiterung, MINOR-Bump erforderlich
- `version:patch` => Fix/Refactor/Docs/CI/Tooling, PATCH-Bump erforderlich
- `version:none` => keine Versionierung erforderlich (Meta-only)

### Keyword-Labels
- `breaking`, `feature`, `fix`, `refactor`, `docs`, `test`, `ci`, `tooling`, `chore`

### Label-Registry (Name -> Farbe -> Zweck)
| Label | Farbe | Zweck |
|---|---|---|
| `version:major` | `#B60205` | Breaking-Change, MAJOR |
| `version:minor` | `#0E8A16` | Neue kompatible Features, MINOR |
| `version:patch` | `#1D76DB` | Fix/Refactor/Docs/CI/Tooling, PATCH |
| `version:none` | `#D4C5F9` | Keine Versionierung notwendig |
| `breaking` | `#B60205` | Public API/Behavior bricht |
| `feature` | `#0E8A16` | Neue Funktionalitaet |
| `fix` | `#1D76DB` | Bugfix |
| `refactor` | `#FBCA04` | Refactoring ohne Behavior-Change |
| `docs` | `#0075CA` | Dokumentation |
| `test` | `#C2E0C6` | Tests |
| `ci` | `#5319E7` | CI/Workflow |
| `tooling` | `#F9D0C4` | Tools/Skripte |
| `chore` | `#C5DEF5` | Maintenance |

### Automatisierung
- PRs werden ueber `.github/labeler.yml` automatisch gelabelt.
- Issues erhalten Labels ueber `issue-labeler` (Titel/Body Keywords).
- Wenn kein SemVer-Label gesetzt ist, erzwingt `version:none` den Default.
- `versioning-labeler` berechnet die erforderliche Versionierung und setzt das passende `version:*` Label auf PRs.
- GitHub-Label-Setup ist in `tools/versioning/labels.json` dokumentiert.

## 8.2 Quodana (Baseline-only)
- Konfiguration: `qodana.yaml`
- Baseline: `.quodana.baseline.json` (im Repo versioniert)
- CI failt nur bei neuen Findings.

## 9. Version-Anker (retroaktiv festgelegt)
- `1.0.0` -> `d9a6015` (Initial Commit)
- `2.0.0` -> `5255724` (Zip -> Archive Rename)
- `3.0.0` -> `fd03389` (Options/Baseline Rename)
