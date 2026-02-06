# Auto-Labeling And Auto-Versioning

## 1. Purpose
This document defines the deterministic PR labeling and versioning decision flow.
It is the SSOT for label taxonomy, caps, priority, and CI integration.

## 2. Deterministic Label Caps
Per PR run, the auto-labeler enforces:
- Exactly one `version:*` label
- Exactly one primary label: `breaking|feature|fix|refactor|ci|test|docs|tooling|chore`
- At most one `impl:*` label
- At most two `area:*` labels

No additional keyword label group is used.

## 3. Versioning Source Of Truth
`tools/versioning/check-versioning.sh` determines required bump semantics.

Console and summary outputs include:
- `required=<major|minor|patch|none>`
- `actual=<major|minor|patch|none>`
- `reason=<deterministic-reason>`

## 4. Priority Rules
Primary label priority is fixed:
`breaking > feature > fix > refactor > ci > test > docs > tooling > chore`

## 5. Mapping Rules
### 5.1 impl:* (max 1)
- `impl:security` -> security/vuln focused changes
- `impl:docs` -> docs-only implementation focus
- `impl:config` -> CI/config/tooling/versioning mechanics
- `impl:quality` -> source/tests quality/refactoring focus

### 5.2 area:* (max 2)
- `area:pipeline` -> `.github/workflows/**`
- `area:qodana` -> `qodana.yaml`, `.qodana/**`
- `area:archive` -> archive internals/processing
- `area:hashing` -> deterministic hashing
- `area:detection` -> detection engine
- `area:materializer` -> materialization flow
- `area:versioning` -> `docs/versioning/**`, `Directory.Build.props`
- `area:tests` -> `tests/**`
- `area:docs` -> `docs/**`, `README.md`
- `area:tooling` -> `tools/**`

## 6. End-To-End Flow
```mermaid
flowchart TD
  A["PR Event"] --> B["Collect changed files + existing labels"]
  B --> C["Run versioning guard"]
  C --> D["Compute deterministic decision JSON"]
  D --> E["Validate decision schema"]
  E --> F["Remove stale auto-labels"]
  F --> G["Apply new labels with caps"]
  G --> H["Write artifact + job summary"]
```

## 7. Primary Decision Flow
```mermaid
flowchart TD
  A["Input: required bump, title, changed files"] --> B{"required == major?"}
  B -- Yes --> C["primary=breaking"]
  B -- No --> D{"src changed or feat keyword?"}
  D -- Yes --> E["primary=feature"]
  D -- No --> F{"fix keyword?"}
  F -- Yes --> G["primary=fix"]
  F -- No --> H{"refactor keyword?"}
  H -- Yes --> I["primary=refactor"]
  H -- No --> J{"ci changed?"}
  J -- Yes --> K["primary=ci"]
  J -- No --> L{"tests changed?"}
  L -- Yes --> M["primary=test"]
  L -- No --> N{"docs changed?"}
  N -- Yes --> O["primary=docs"]
  N -- No --> P{"tools changed?"}
  P -- Yes --> Q["primary=tooling"]
  P -- No --> R["primary=chore"]
```

## 8. Sequence Diagram
```mermaid
sequenceDiagram
  participant GH as "GitHub"
  participant CI as "CI Workflow"
  participant Guard as "versioning guard"
  participant Engine as "compute-pr-labels.js"
  participant API as "GitHub Labels API"

  GH->>CI: pull_request_target event
  CI->>Guard: compute required/actual bump
  CI->>Engine: files + labels + guard result
  Engine-->>CI: decision.json
  CI->>CI: schema validation
  CI->>API: remove stale auto-labels
  CI->>API: add deterministic labels
  CI-->>GH: artifact + summary
```

## 9. Version Guard Decision Flow
```mermaid
flowchart TD
  A["Run check-versioning.sh"] --> B{"guard successful?"}
  B -- Yes --> C["Use required + actual from guard"]
  B -- No --> D["Fallback required=none actual=none"]
  C --> E{"actual satisfies required?"}
  D --> F["reason=guard-unavailable-fallback"]
  E -- Yes --> G["Pass and label with reason"]
  E -- No --> H["Fail in preflight versioning-guard"]
```

## 10. Examples
### Docs-only PR
- `version:none`, `primary=docs`, optional `impl:docs`, `area:docs`

### CI-only PR
- `version:patch`, `primary=ci`, `impl:config`, `area:pipeline`

### Source + tests + docs PR
- `version:minor` (or `major` if breaking), single primary by priority, capped area labels

## 11. Regression Safety
Golden testcases live in:
- `tools/versioning/testcases/*.json`

Validation scripts:
- `tools/versioning/test-compute-pr-labels.js`
- `tools/versioning/validate-label-decision.js`
