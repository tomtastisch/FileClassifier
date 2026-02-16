# Documentation Standard (SSOT)

## 1. Purpose
This standard defines structure, naming, and maintenance rules for all Markdown documents in the repository.
The goal is consistent, auditable, and quickly scannable documentation.

## 2. Scope
- `README.md`
- `docs/**/*.md`
- module-local `README.md` under `src/**` and `tests/**`

## 3. Naming Rules
- Root and core documents under `docs/` use `UPPER_SNAKE_CASE.md`.
- Subfolders keep the existing domain structure:
  - `docs/versioning/`
  - `docs/guides/`
  - `docs/governance/`
  - `docs/verification/`
- `UPPER_SNAKE_CASE.md` applies in subfolders as well (exception: `README.md` as entry point).

## 4. Required Structure per Document
Each domain document follows this order:
1. purpose
2. scope
3. rules/architecture
4. verification/evidence
5. boundaries/non-goals
6. linked SSOT sources

## 5. Language and Style
- Primary language: German.
- Technical terms stay in their common form (`CI`, `Qodana`, `SemVer`, `SSOT`).
- Formulate statements as deterministically and verifiably as possible.

## 6. Linking Rules
- `README.md` links to `docs/101_INDEX_CORE.MD` as the central entry point.
- `docs/101_INDEX_CORE.MD` links to all SSOT documents.
- Each specialized doc links back to `docs/101_INDEX_CORE.MD`.

## 7. Diagram Standard
- For flows: `flowchart` or `sequenceDiagram` (Mermaid).
- Always quote nodes with special characters: `["..."]`.
- Diagrams must reflect the real CI/code flow, not generic placeholders.

## 8. Maintenance Process
For changes to CI, labeling, versioning, or API:
1. adjust implementation.
2. update the related SSOT documentation in the same commit.
3. run link and structure checks.

Pflichtcheck:
```bash
python3 tools/check-doc-consistency.py
python3 tools/check-docs.py
```

## 9. Non-Goals
- No duplicated maintenance of the same rule across multiple SSOT files.
- No outdated backup/deprecated documents in the active path.
- No mixed naming schemes (`kebab-case`/`snake_case`) within the same documentation level.

## 10. CI Checklist: Doc Consistency (binding)
The following checklist is wired as a CI rule and must pass on every run:
1. Outdated core file name patterns are forbidden and must point to canonical targets.
2. Outdated guide names are forbidden and must point to canonical guide files.
3. Outdated guide index references are forbidden and must point to the canonical guides index.
4. Non-canonical `docs/04_*` paths are forbidden; only current 3-digit UPPERCASE paths are allowed.
5. Each hit is reported with `file:line`, rule ID, and concrete remediation help.

## RoC References
- [Artifact contract rule](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/artifact_contract.yaml)
- [Docs drift rule](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/docs_drift.yaml)
- [Shell safety rules](https://github.com/tomtastisch/FileClassifier/blob/main/tools/ci/policies/rules/shell_safety.yaml)
