# GitHub Copilot: Repo-Instructions (SSOT fuer Copilot)

Kurz: Diese Datei ist die verbindliche Arbeitsanweisung fuer GitHub Copilot (Chat/Coding Agent) in diesem Repo.
Sie darf `AGENTS.md` nicht widersprechen. Im Konfliktfall gilt immer `AGENTS.md`.

## 1) Geltung & Prioritaet
- Scope: GitHub Copilot (Chat/Coding Agent).
- Non-Goal: Keine Aenderung der menschlichen Maintainer-Prozesse; keine Anweisung, Repo-Governance zu umgehen.
- Regeln:
  - Wenn eine Anweisung hier fehlt: `AGENTS.md` ist die naechste Referenz.
  - Wenn diese Datei und `AGENTS.md` widersprechen: Folge `AGENTS.md` (fail-closed).

## 2) Non-Negotiables (fail-closed)
- Terminal-first: Diagnose/Fixes ausschliesslich via CLI, reproduzierbar und auditierbar.
- Keine Secrets in Logs oder in Markdown-Dokumenten (auch nicht redacted-by-hand).
- `SECURITY.md` ist eingefroren und darf nicht geaendert werden.
- Keine destruktiven Git-Kommandos ohne explizite Freigabe (z. B. `git reset --hard`).
- Merge nur, wenn:
  - required checks gruen,
  - alle Review-Threads `resolved` (inkl. outdated),
  - Code-Scanning offene Alerts = `0`,
  - Version-Konvergenz gemaess `AGENTS.md` erfuellt ist.

## 3) Branch/PR-Konventionen (Repo-Contract)
- WIP=1: genau ein Thema pro Branch/PR.
- Branch-Namen: `codex/<tag>/<kurztext-kebab>`
  - erlaubte `<tag>`: `fix|release|feat|refactor|docs|test|ci|chore|security`
- PR-Titel: `<tag>(<scope>): <deutsche kurzbeschreibung>`
- PR-Beschreibung: Deutsch und nach `AGENTS.md` Pflichtstruktur + Checklisten + Evidence + DoD-Matrix.

## 4) Arbeitsablauf (push&green)
1. Branch erstellen (siehe Konventionen).
2. Aenderungen implementieren (klein, fokussiert).
3. Lokal mindestens Preflight ausfuehren (wenn moeglich):
   - `bash tools/ci/bin/run.sh preflight`
4. Commit(s) klein und atomar; keine Misch-Themen.
5. Push:
   - `git push -u origin HEAD`
6. PR erstellen (CLI bevorzugt):
   - `gh pr create --fill`
7. CI abwarten und evidenzbasiert reagieren:
   - `gh pr checks <NR> --watch`
8. Review-Fixes iterativ einarbeiten; Threads auf `resolved` setzen.
9. Merge erst nach allen Gates (siehe oben).

## 5) CI-Diagnose (wenn rot)
- Status:
  - `gh pr checks <NR>`
  - `gh run list -b <branch> -L 10`
  - `gh run view <RUN_ID> --log-failed`
- Repo-Tooling (lokal):
  - `bash tools/ci/bin/run.sh preflight`
  - `bash tools/ci/bin/run.sh build`
  - `bash tools/ci/bin/run.sh tests-bdd-coverage`

## 6) Code Scanning / CodeQL (Missstaende vermeiden)
- CodeQL muss Advanced-Setup bleiben; Default-Setup darf nicht aktiviert werden.
  - Guardrail lokal: `bash tools/ci/check-codeql-default-setup.sh`
- CodeQL Query-Set ist security-only (keine Quality-Alerts als Code-Scanning Alerts).
- Vor Merge verifizieren (open alerts = 0):
  - `gh api 'repos/{owner}/{repo}/code-scanning/alerts?state=open' --paginate | jq -s 'map(length)|add'`

## 7) Evidence-Policy (Pflicht)
- Jede materielle Aussage muss Evidence haben (Kommando + Artefakt/Output + Pfad).
- Unklarheiten immer als `ASSUMPTION` markieren und ein minimales Verifikationskommando angeben.

## 8) Stop-Condition
- Wenn ein Fix-Zyklus 2x denselben Fehler reproduziert, ohne neue Evidence: STOP, Fehlerklasse neu bewerten und
  gezielt neue Evidence anfordern.
