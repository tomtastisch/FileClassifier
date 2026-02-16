# Policy Branch/PR Naming (DE)

## 1. Purpose
This policy defines a consistent, professional, and machine-checkable naming scheme for branches and pull requests.

## 2. Binding Conventions

### 2.1 Branches
- Required format: `codex/<tag>/<shorttext-kebab>`
- Transition (legacy, temporary): `codex/<tag>-<shorttext-kebab>` is accepted for existing branches.
- Allowed `<tag>` values:
  - `fix`
  - `release`
  - `feat`
  - `refactor`
  - `docs`
  - `test`
  - `ci`
  - `chore`
  - `security`
- `<shorttext-kebab>`:
  - only `a-z`, `0-9`, `-`
  - no spaces, no special characters
  - at least 3 characters

### 2.2 PR-Titel
- Required format: `<tag>(<scope>): <german short description>`
- `tag`: as above.
- `scope`:
  - only `a-z0-9-`
  - optional, but recommended.
- Short description:
  - German (mandatory),
  - precise (no placeholder text),
  - describes outcome/change.

### 2.3 PR-Beschreibung
- Must be written in German.
- Must follow the repository PR template structure.
- Must include checklists and a DoD matrix.

## 3. Forbidden Patterns
- Imprecise titles like `update`, `changes`, `misc`.
- Branches without a tag segment.
- PRs without checklist maintenance after review fixes.

## 4. Enforcement
- CI check `pr-governance` validates:
  - branch format,
  - PR title format,
  - mandatory sections and checklists in the PR description.
- Fail-closed: violations block merging.

## 5. DoD
- DoD A: `pr-governance` completes successfully for the PR.
- DoD B: branch and PR names match the convention exactly.

## 6. Rule Mapping
- Normative rule file: `tools/ci/policies/rules/naming_snt.yaml`
