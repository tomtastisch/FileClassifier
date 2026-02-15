# Autoritative Copilot-Arbeitsanweisung (Single Source of Truth)

Kurz: Diese Datei ist die einzige verbindliche Arbeitsanweisung für GitHub Copilot / Coding Agent in diesem Repo. Alle
anderen Guideline- oder Instruction-Dateien dürfen nur noch auf diese Datei verweisen.

1) Gültigkeit und Priorität

- Diese Datei ist die alleinige Autorität für Copilot-Verhalten in diesem Repository.
- Andere Guideline-Dateien müssen entweder entfernt werden oder nur noch einen Kurzverweis auf diese Datei enthalten.

2) Grundprinzipien (normativ)

- Terminal-first: Alle Git- und CI-Aktionen sowie Diagnose- und Fix-Schritte erfolgen ausschließlich über
  Terminal/CLI-Kommandos. Keine Aufforderungen an den Nutzer, wiederholt manuell Terminal-Schritte auszuführen.
- Branch-Erstellung: Copilot legt selbstständig den Arbeitsbranch an. Namensschema: jira-<ISSUE>-<kurz>-<timestamp> oder
  repo-konformes Muster. (Beispiel: jira-1234-fix-ci-20260103T1500Z)
- Commit-Disziplin: Kleine, atomare Commits mit klarer Message; Commit-Body enthält Evidence-Trailer (siehe
  Evidence-Policy).
- Push-Gate: Push erfolgt ausschließlich über das repo-definierte Tool: gh ci-push. Niemals direkt git push.
- CI-Abschlusskriterium: Arbeit gilt erst als fertig, wenn nach gh ci-push alle relevanten CI-Jobs grün sind ("
  push&green").

3) Push- und CI-Prozess (verbindlich)

- Ablauf:
  1. Branch anlegen.
  2. Änderungen lokal per Terminal durchführen und Tests/Checks lokal ausführen (so weit möglich).
  3. Commit mit Evidence-Placeholder (siehe Evidence-Policy).
  4. Push: gh ci-push
  5. Warten, CI-Status prüfen (maschinell/Terminal).
  6. Bei Failure: Logs herunterladen/lesen → Diagnose-Commands ausführen → minimaler Fix → gh ci-push erneut.
  7. Wiederholen bis CI grün oder Stop-Condition greift.
- Status-Tools (maschinenlesbar): ci-status-json (sofern im Repo vorhanden) / ci-dashboard (optional für Humans).
- Diagnose-Commands (Pflicht bei Failure, wenn verfügbar):
  - ./tools/ci/ci-last-failed-log.sh
  - ./tools/ci/ci-download-artifacts-last-run.sh
  - Falls es im Repo andere diagnostische Tools gibt, nutze diese entsprechend.
- Stop-Condition: Wenn ein Fix-Zyklus 2× ohne neue Evidenz denselben Fehler reproduziert → STOP; Fehler neu
  klassifizieren und zusätzliche Evidenz anfordern.

4) Evidence-Policy (Pflicht in Commit-Message / PR-Description)

- Jedes Commit oder PR, das Code/Config ändert, muss Evidence enthalten:
  - Evidence: <relevante Logzeile oder Command-Output>
  - Class: <Fehlerklasse (Parse/Config, Build, Dependency, Runtime, Test, Orchestration)>
  - Rationale: <warum der Fix die Evidence adressiert>
- PR-Template muss die folgenden Felder ausfüllen:
  - Autoritative Guideline: .github/copilot-instructions.md
  - CI Evidence (Run URL/ID)
  - Command used: gh ci-push
- Die PR-Beschreibung muss die Run-ID/URL der grünen CI ausweisen, wenn abgeschlossen.

5) Deterministische Fehleranalyse (Kurz, verpflichtend)

- D-1 Fail-Fast Klassifikation: Erste harte Fehlermeldung einer Fehlerklasse zuordnen.
- D-2 Minimal-Repro/Proof Step: Vor Fix eine minimale Reproduktionsprüfung definieren und (wenn nötig) als Tool im Repo
  anlegen.
- D-3 Hypothesenlimit: Max. 2 Hypothesen gleichzeitig; jede mit speziellem Verifikations-Command.
- D-4 Patch-Regel: Fix erst nach D-2; Patches klein und reversibel.
- D-5 Regression-Gate: Nach Fix D-2 erneut ausführen, dann push&green.

6) Externe Inputs / Ausnahmen

- Copilot darf den Nutzer nur dann um Hilfe bitten, wenn ein zwingend externer Input fehlt (z. B. Secrets, Token,
  manuelle Freigabe, nicht verfügbare Credentials).
- In diesem Fall: präzise Anforderung formulieren (welches Secret/Token, warum, welches Scope/expire, wie sicher zu
  liefern). Keine generischen Aufforderungen.

7) Self-Check vor Abschluss (Pflicht)

- Vor endgültigem PR-Status:
  - Führe gh ci-push aus.
  - Warte auf Abschluss; dokumentiere Run-ID/URL.
  - Wenn Failure: logs herunterladen, minimalen Fix anwenden, wieder gh ci-push.
  - Wiederholen bis grün oder Stop-Condition.
- In der PR-Beschreibung: Evidence (Run-ID/URL), welche Jobs grün sind, welches Command verwendet wurde (gh ci-push).

8) Governance / Integritäts-Hinweis

- Änderungen an dieser Datei sind kritisch: Wenn diese Datei entfernt/umbenannt wird, muss ein Governance-Guard (
  CI-Check) aktiv werden. Falls ein solcher Guard noch nicht existiert, wird empfohlen, kurzfristig einen CI-Step
  einzurichten, der das Vorhandensein dieser Datei validiert.

9) Aufforderung an Copilot (konkret, maschinenlesbar)

- "Arbeitsmodus: vollständig autonom im Branch. Erstelle Branch, implementiere Änderungen, führe Tests/Checks via
  Terminal aus, commit + push via `gh ci-push`, analysiere CI-Failures mit repo-Tools, implementiere minimal fixes und
  wiederhole push&green bis CI grün ist. Nur bei fehlenden externen Inputs explizit anfragen."

10) Hinweise für Repo-Maintainer

- Stelle sicher, dass:
  - PR-Template Felder für Evidence und Command enthält.
  - Es keine anderen widersprüchlichen Anweisungen im Repo gibt (alte Dateien entfernen oder auf diese Datei verweisen).
  - Tools für Diagnose (./tools/ci/...) vorhanden und ausführbar sind oder ergänzt werden.

(Ende)