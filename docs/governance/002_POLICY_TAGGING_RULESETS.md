# POLICY: GitHub Tag Rulesets `tags-stable` + `tags-rc`

## Scope / Target
Tags are SSOT for publish. Patterns:
- stable: `v*` (exclude `v*-rc.*` if UI supports excludes)
- rc: `v*-rc.*`

## Rulesets (GitHub UI)
Create rulesets at https://github.com/tomtastisch/FileClassifier/settings/rules/new?target=tag

### Ruleset `tags-stable`
- Target: tag refs
- Pattern:
  - include: `v*`
  - exclude: `v*-rc.*` (only if UI supports excludes; otherwise `tags-rc` is stricter and will match rc tags too)
- Enforcement: Active (fail-closed)
- Bypass: minimal (Admins or small Maintainer team). No broad bot/app bypass unless explicitly required.
- Rules to enable (depending on UI availability):
  - Restrict/Prevent deletions
  - Restrict/Prevent updates / force pushes / moving tags
  - (optional) Restrict creations (only if you want to limit who can create release tags)
  - (optional) Require signed commits / signed tags (only if available)
  - (later) Require status checks (enable only after tag workflows exist and are stable)

### Ruleset `tags-rc`
- Target: tag refs
- Pattern: `v*-rc.*`
- Enforcement and bypass: same as `tags-stable`
- Rules:
  - Restrict/Prevent deletions
  - Restrict/Prevent updates / force pushes / moving tags
  - (optional) Restrict creations
  - (optional) Require signed commits / signed tags
  - Require status checks once the rc tag workflow is stable (RC is fail-closed)

## Expected behaviour
- Stable and RC tags can’t be moved or deleted (without bypass).
- Stable and RC tags become the auditable SSOT for releases/publish.
- Publish workflows must depend on the tag gate.

## Ordering / rollout
1. Create rulesets (in evaluate mode only if needed, otherwise active).
2. Add `.github/workflows/tag-gate.yml`.
3. Wire publish pipelines to `needs: tag-gate` or via `workflow_run` on success.
4. Enable status checks for tags only once the checks exist and are stable.
