# Codex Operating Rules (Repository Contract)

## 1) Ziel und Leitlinie
- Deterministisch, evidenzbasiert, fail-closed arbeiten.
- Korrektheit, Reproduzierbarkeit, Security und Auditierbarkeit vor Geschwindigkeit.
- `SECURITY.md` ist eingefroren und darf nicht geaendert werden.

## 2) Pflicht-Struktur fuer substanzielle Antworten
1. GOAL & SCOPE
2. DEFINITIONS & ASSUMPTIONS
3. PLAN (STEPS + CHECKS)
4. EXECUTION (COMMANDS / FILE CHANGES)
5. VERIFICATION & EVIDENCE
6. RESULT & DECISION LOG
7. RISKS / OPEN ITEMS / NEXT STEPS (PRIORITIZED)

## 3) Iteratives Arbeitsmodell (verbindlich)
- WIP-Limit 1: genau ein Thema pro Branch/PR.
- Branch-Namensschema:
  - `codex/<tag>/<kurztext-kebab>`
  - erlaubte `<tag>`: `fix|release|feat|refactor|docs|test|ci|chore|security`
- PR-Titelschema:
  - `<tag>(<scope>): <deutsche kurzbeschreibung>`
- Nach jedem Push:
  - auf alle erforderlichen Checks warten,
  - alle Review-Kommentare abarbeiten,
  - alle Threads auf `resolved` setzen (inkl. outdated).
- Verbindliche Review-Regel:
  - Ein Thread darf nur auf `resolved` gesetzt werden, wenn der Inhalt fachlich bearbeitet wurde.
  - Zulaessige Bearbeitung ist genau eine der folgenden Varianten:
    - Code-/Test-/Dokuaenderung im PR mit nachvollziehbarer Evidence.
    - begruendete Widerlegung als `ASSUMPTION` + Verifikationsnachweis, warum keine Aenderung noetig ist.
  - Unzulaessig: Threads ohne Bearbeitung nur aus Prozessgruenden zu resolven.
- Merge nur wenn:
  - required checks gruener Status,
  - keine offenen Review-Threads,
  - PR laut Ruleset/Branch-Protection mergebar,
  - Code-Scanning-Toolstatus fuer offene Alerts ist `0` (kein offener Alert).
- Nach Merge:
  - neuesten `main`-Run auf `success` verifizieren,
  - naechstes Thema in neuem Branch starten.

## 4) PR-Beschreibung (verbindlich)
- PR-Beschreibung auf Deutsch.
- Muss enthalten:
  - Ziel/Scope,
  - umgesetzte Aufgaben als Checkliste,
  - Nachbesserungen als Checkliste,
  - Evidence (Befehle/Artefakte),
  - DoD-Matrix mit mindestens zwei auditierbaren DoD je umgesetztem Punkt.
- Bei Review-Fixes wird die PR-Beschreibung aktiv aktualisiert.

## 5) Fail-Closed und Live-API-Regeln
- Blocker-Checks muessen bestehen; kein stilles Bypass.
- `unknown` nur fuer explizite report-only Claims.
- Live-API-Claims sind blocker:
  - Retry + Exponential Backoff,
  - reason codes (`auth|rate-limit|network|5xx|unknown`),
  - finaler Fehler => `fail`.

## 6) Evidence-Pflicht
- Jede materielle Aussage benoetigt:
  - Kommando,
  - Artefakt/Log,
  - Datei-/Pfadangabe.
- Nicht verifizierbare Aussagen sind `ASSUMPTION` plus Verifikationskommando.

## 7) Security- und Workflow-Guardrails
- GitHub Actions mit Least Privilege:
  - top-level read-only,
  - write nur job-lokal bei Bedarf.
- Actions nur SHA-gepinnt.
- Keine Secret-Werte in Logs.
- Kein `pull_request_target` + Secrets ohne explizite Freigabe.
- Scorecard/Governance-Drift wird nicht ignoriert; offene Alerts sind vor Merge zu schliessen.

## 8) Versionierungs-Konvergenz (blocker)
Die folgenden Werte muessen immer identisch sein:
- `Directory.Build.props` -> `RepoVersion`
- `src/FileTypeDetection/FileTypeDetectionLib.vbproj` -> `Version`
- `src/FileTypeDetection/FileTypeDetectionLib.vbproj` -> `PackageVersion`
- Top-Eintrag in `docs/versioning/002_HISTORY_VERSIONS.MD`
- neuestes GitHub-Release-Tag
- neueste NuGet-Paketversion

Abweichung ist blocker und vor Merge/Release zu beheben.

## 9) Scope/Safety
- Keine Ruecknahme fremder, nicht beauftragter Aenderungen.
- Keine destruktiven Git-Befehle ohne explizite Freigabe.
- Aenderungen klein, fokussiert, nachvollziehbar.
- Unerwarteter Drift => stoppen und klaeren.

## 10) Lokale Overrides
- Optional: `AGENTS.override.md` wird zusaetzlich angewendet.
- `AGENTS.override.md` ist lokal-only und darf nicht committed werden.
