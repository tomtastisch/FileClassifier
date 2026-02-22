<!-- LANG_SWITCH:BEGIN -->
[DE](../../0_de/guides/000_INDEX_GUIDES.MD) | [EN](000_INDEX_GUIDES.MD)
<!-- LANG_SWITCH:END -->

# Guides - Change Playbooks

## 1. Purpose
This level under `docs/guides/` provides practical playbooks for recurring changes:
- create or adjust options
- add new datatypes/models cleanly

The goal is that developers and consumers can see without back-and-forth:
1. where changes must be made,
2. when the change becomes active,
3. how the change is verified.

## 2. Unified Format (for all guides)
All guides follow the same structure:
1. purpose and scope
2. central file map
3. step-by-step checklist
4. concrete example
5. flowchart + sequence
6. verification + done criteria

## 3. Playbooks
1. [Playbook: Create and adjust options](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/101_GUIDE_OPTIONS.MD)
2. [Playbook: Extend new datatypes and API models](https://github.com/tomtastisch/FileClassifier/blob/main/docs/guides/102_GUIDE_DATATYPE.MD)
3. Playbook: Migrate legacy NuGet to canonical package (`docs/guides/104_GUIDE_MIGRATE_LEGACY_NUGET.MD`)

## 4. Which guide when?
| Question | Guide |
|---|---|
| New or changed configuration option? | `docs/guides/101_GUIDE_OPTIONS.MD` |
| New detectable file type (`FileKind`) or magic/alias update? | `docs/guides/102_GUIDE_DATATYPE.MD` |
| New API return model under `Abstractions/*`? | `docs/guides/102_GUIDE_DATATYPE.MD` |
| Combination of option + datatype? | both guides, first options guide, then datatype guide |

## 5. Links
- [Documentation index](https://github.com/tomtastisch/FileClassifier/blob/main/docs/101_INDEX_CORE.MD)
- [01 - API functions](https://github.com/tomtastisch/FileClassifier/blob/main/docs/110_API_CORE.MD)
- [02 - Overall architecture and flows](https://github.com/tomtastisch/FileClassifier/blob/main/docs/120_ARCH_CORE.MD)
- [03 - References](https://github.com/tomtastisch/FileClassifier/blob/main/docs/references/101_REFERENCES_CORE.MD)

## 6. Maintenance Note
The guides are binding working templates. If structural API changes happen, the affected sections must be updated in the same change.

## Documentation Maintenance Checklist
- [ ] Content verified against current code state.
- [ ] Links and anchors checked with `python3 tools/check-docs.py`.
- [ ] Examples/commands verified locally.
- [ ] Terminology aligned with `docs/110_API_CORE.MD`.
