<!-- LANG_SWITCH:BEGIN -->
[DE](004_POLICY_DOCUMENTATION.MD) | [EN](../../1_en/governance/004_POLICY_DOCUMENTATION.MD)
<!-- LANG_SWITCH:END -->

# Dokumentationsstandard (SSOT)

## 1. Zweck
Dieser Standard definiert Aufbau, Benennung und Pflege aller Markdown-Dokumente im Repository.
Ziel ist eine konsistente, auditierbare und schnell erfassbare Dokumentation.

## 2. Geltungsbereich
- `README.md`
- `docs/**/*.md`
- modulnahe `README.md` unter `src/**` und `tests/**`

## 3. Benennungsregeln
- Root- und Kern-Dokumente unter `docs/` in `UPPER_SNAKE_CASE.md`.
- Unterordner behalten bestehende fachliche Struktur:
  - `docs/versioning/`
  - `docs/guides/`
  - `docs/governance/`
  - `docs/verification/`
- Auch in Unterordnern gilt `UPPER_SNAKE_CASE.md` (Ausnahme: `README.md` als Einstieg).

## 4. Pflichtstruktur pro Dokument
Jedes fachliche Dokument folgt dieser Reihenfolge:
1. Zweck
2. Geltungsbereich
3. Regeln/Architektur
4. Verifikation/Nachweise
5. Grenzen/Nicht-Ziele
6. Verlinkte SSOT-Quellen

## 5. Sprache und Stil
- Primärsprache: Deutsch.
- Technische Begriffe bleiben in üblicher Form (`CI`, `Qodana`, `SemVer`, `SSOT`).
- Aussagen möglichst deterministisch und prüfbar formulieren.

## 6. Verlinkungsregeln
- `README.md` verlinkt auf `docs/001_INDEX_CORE.MD` als zentralen Einstieg.
- `docs/001_INDEX_CORE.MD` verlinkt auf alle SSOT-Dokumente.
- Jede Spezialdoku verlinkt zurück auf `docs/001_INDEX_CORE.MD`.

## 7. Diagrammstandard
- Für Abläufe: `flowchart` oder `sequenceDiagram` (Mermaid).
- Knoten mit Sonderzeichen immer quoten: `["..."]`.
- Diagramme müssen den tatsächlichen CI-/Code-Fluss abbilden, keine generischen Platzhalter.

## 8. Pflegeprozess
Bei Änderungen an CI, Labeling, Versioning oder API:
1. Implementierung anpassen.
2. Zugehörige SSOT-Doku im selben Commit aktualisieren.
3. Link- und Strukturprüfung ausführen.

Pflichtcheck:
```bash
python3 tools/check-doc-consistency.py
python3 tools/check-docs.py
```

## 9. Nicht-Ziele
- Keine doppelte Pflege derselben Regel in mehreren SSOT-Dateien.
- Keine veralteten Backup-/Deprecated-Dokumente im aktiven Pfad.
- Keine gemischten Benennungsschemata (`kebab-case`/`snake_case`) innerhalb derselben Doku-Ebene.

## 10. CI-Checkliste: Doc-Consistency (verbindlich)
Die folgende Checkliste ist als CI-Regel verdrahtet und muss bei jedem Lauf bestehen:
1. Veraltete Kern-Dateinamenmuster sind verboten und müssen auf kanonische Ziele zeigen.
2. Veraltete Guides-Namen sind verboten und müssen auf kanonische Guide-Dateien zeigen.
3. Veraltete Guide-Index-Referenzen sind verboten und müssen auf den kanonischen Guides-Index zeigen.
4. Nicht-kanonische `docs/04_*`-Pfade sind verboten; erlaubt sind nur aktuelle 3-stellige UPPERCASE-Pfade.
5. Jeder Treffer wird mit `Datei:Zeile`, Regel-ID und konkreter Korrekturhilfe ausgegeben.

## RoC-Bezug
- [Artifact-Contract-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs-Drift-Regel](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
- [Shell-Safety-Regeln](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/shell_safety.yaml)
