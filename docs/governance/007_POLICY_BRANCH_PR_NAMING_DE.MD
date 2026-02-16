<!-- LANG_SWITCH:BEGIN -->
[DE](007_POLICY_BRANCH_PR_NAMING_DE.MD) | [EN](107_POLICY_BRANCH_PR_NAMING_DE.MD)
<!-- LANG_SWITCH:END -->

# Policy Branch/PR Naming (DE)

## 1. Zweck
Diese Policy definiert ein einheitliches, professionelles und maschinenpruefbares Naming fuer Branches und Pull Requests.

## 2. Verbindliche Konventionen

### 2.1 Branches
- Muss-Format: `codex/<tag>/<kurztext-kebab>`
- Uebergang (legacy, befristet): `codex/<tag>-<kurztext-kebab>` wird fuer bestehende Branches akzeptiert.
- Erlaubte `<tag>`:
  - `fix`
  - `release`
  - `feat`
  - `refactor`
  - `docs`
  - `test`
  - `ci`
  - `chore`
  - `security`
- `<kurztext-kebab>`:
  - nur `a-z`, `0-9`, `-`
  - keine Leerzeichen, keine Sonderzeichen
  - mindestens 3 Zeichen

### 2.2 PR-Titel
- Muss-Format: `<tag>(<scope>): <deutsche kurzbeschreibung>`
- `tag` wie oben.
- `scope`:
  - nur `a-z0-9-`
  - optional, aber empfohlen.
- Kurzbeschreibung:
  - Deutsch,
  - praezise, kein Platzhaltertext,
  - Ergebnis-/Aenderungsbezug.

### 2.3 PR-Beschreibung
- Muss auf Deutsch sein.
- Muss die Repository-PR-Template-Struktur einhalten.
- Muss Checklisten und DoD-Matrix enthalten.

## 3. Verbotene Muster
- Unpraezise Titel wie `update`, `changes`, `misc`.
- Branches ohne Tag-Segment.
- PRs ohne Checklistenpflege nach Nachbesserungen.

## 4. Durchsetzung
- CI-Check `pr-governance` validiert:
  - Branch-Format,
  - PR-Titel-Format,
  - Pflichtsektionen und Checklisten in der PR-Beschreibung.
- Fail-closed: Verstoss blockiert Merge.

## 5. DoD
- DoD A: `pr-governance` laeuft erfolgreich fuer die PR.
- DoD B: Branch- und PR-Name entsprechen exakt der Konvention.

## 6. Rule Mapping
- Normative Rule-Datei: `tools/ci/policies/rules/naming_snt.yaml`
