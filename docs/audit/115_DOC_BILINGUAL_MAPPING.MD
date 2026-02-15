# Bilingual Documentation: DE<->EN Mapping (0NN_ -> 1NN_)

## 1. Goal & Scope
This report documents, deterministically, the mapping of all documents following the `NNN_*` naming scheme (German is primary) to their English mirror documents `1NN_*`.

Scope:
- Applies to all documents named `NNN_<slug>.md` or `NNN_<slug>.MD` (independent of folder).
- EN is a semantic translation of the DE version (same structure/sections; code blocks/commands identical).

Non-goals:
- No code/build/CI changes (except Markdown link fixes inside `.md/.MD`).

## 2. Naming Rules (SSOT)
- DE file: `0NN_<slug>.<ext>` with a three-digit number `0NN` (e.g. `030_...`).
- EN file: `1NN_<slug>.<ext>`.
- `NN` = last two digits of the DE number.
- `slug` and extension remain identical (deterministic mapping).

Mapping function:
- `0NN_*` -> `1NN_*` (same directory, same `slug`, same extension).

## 3. Inventory + Mapping Table
| DE_PATH | EN_PATH | STATUS | NOTES |
|---|---|---|---|
| `docs/001_INDEX_CORE.MD` | `docs/101_INDEX_CORE.MD` | exists |  |
| `docs/010_API_CORE.MD` | `docs/110_API_CORE.MD` | exists |  |
| `docs/020_ARCH_CORE.MD` | `docs/120_ARCH_CORE.MD` | exists |  |
| `docs/021_USAGE_NUGET.MD` | `docs/121_USAGE_NUGET.MD` | exists |  |
| `docs/audit/000_HASHING_BASELINE.MD` | `docs/audit/100_HASHING_BASELINE.MD` | exists |  |
| `docs/audit/000_INDEX.MD` | `docs/audit/100_INDEX.MD` | exists |  |
| `docs/audit/002_AUDIT_CONTRACT_AND_GUARDRAILS.MD` | `docs/audit/102_AUDIT_CONTRACT_AND_GUARDRAILS.MD` | exists |  |
| `docs/audit/003_SECURITY_ASSERTION_TRACEABILITY.MD` | `docs/audit/103_SECURITY_ASSERTION_TRACEABILITY.MD` | exists |  |
| `docs/audit/004_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD` | `docs/audit/104_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD` | exists |  |
| `docs/audit/005_CODE_ANALYSIS_METHOD.MD` | `docs/audit/105_CODE_ANALYSIS_METHOD.MD` | exists |  |
| `docs/audit/006_CODE_REVIEW_FINDINGS.MD` | `docs/audit/106_CODE_REVIEW_FINDINGS.MD` | exists |  |
| `docs/audit/007_THREAT_MODEL.MD` | `docs/audit/107_THREAT_MODEL.MD` | exists |  |
| `docs/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD` | `docs/audit/108_INCIDENT_RESPONSE_RUNBOOK.MD` | exists |  |
| `docs/audit/009_SUPPLY_CHAIN_BASELINE.MD` | `docs/audit/109_SUPPLY_CHAIN_BASELINE.MD` | exists |  |
| `docs/audit/010_REFACTOR_BACKLOG.MD` | `docs/audit/110_REFACTOR_BACKLOG.MD` | exists |  |
| `docs/audit/011_SECURITY_BENCHMARK.MD` | `docs/audit/111_SECURITY_BENCHMARK.MD` | exists |  |
| `docs/audit/012_WAVE_EXECUTION_DOD.MD` | `docs/audit/112_WAVE_EXECUTION_DOD.MD` | exists |  |
| `docs/audit/013_SCORECARD_GOVERNANCE_ALERT_MAPPING.MD` | `docs/audit/113_SCORECARD_GOVERNANCE_ALERT_MAPPING.MD` | exists |  |
| `docs/audit/014_EVIDENCE_REPORT_ISSUE_67.MD` | `docs/audit/114_EVIDENCE_REPORT_ISSUE_67.MD` | exists |  |
| `docs/ci/001_PIPELINE_CI.MD` | `docs/ci/101_PIPELINE_CI.MD` | exists |  |
| `docs/ci/002_NUGET_TRUSTED_PUBLISHING.MD` | `docs/ci/102_NUGET_TRUSTED_PUBLISHING.MD` | exists |  |
| `docs/contracts/001_CONTRACT_HASHING.MD` | `docs/contracts/101_CONTRACT_HASHING.MD` | exists |  |
| `docs/governance/001_POLICY_CI.MD` | `docs/governance/101_POLICY_CI.MD` | exists |  |
| `docs/governance/002_POLICY_LABELING.MD` | `docs/governance/102_POLICY_LABELING.MD` | exists |  |
| `docs/governance/002_POLICY_NAMING_UNIFIED.MD` | `docs/governance/102_POLICY_NAMING_UNIFIED.MD` | exists |  |
| `docs/governance/003_INDEX_GOVERNANCE.MD` | `docs/governance/103_INDEX_GOVERNANCE.MD` | exists |  |
| `docs/governance/003_POLICY_VERSIONING_SVT.MD` | `docs/governance/103_POLICY_VERSIONING_SVT.MD` | exists |  |
| `docs/governance/004_POLICY_DOCUMENTATION.MD` | `docs/governance/104_POLICY_DOCUMENTATION.MD` | exists |  |
| `docs/governance/005_POLICY_NAMING.MD` | `docs/governance/105_POLICY_NAMING.MD` | exists |  |
| `docs/governance/006_INDEX_CI_RULES.MD` | `docs/governance/106_INDEX_CI_RULES.MD` | exists |  |
| `docs/governance/007_POLICY_BRANCH_PR_NAMING_DE.MD` | `docs/governance/107_POLICY_BRANCH_PR_NAMING_DE.MD` | exists |  |
| `docs/guides/000_INDEX_GUIDES.MD` | `docs/guides/100_INDEX_GUIDES.MD` | exists |  |
| `docs/guides/001_GUIDE_OPTIONS.MD` | `docs/guides/101_GUIDE_OPTIONS.MD` | exists |  |
| `docs/guides/002_GUIDE_DATATYPE.MD` | `docs/guides/102_GUIDE_DATATYPE.MD` | exists |  |
| `docs/guides/003_GUIDE_PORTABLE.MD` | `docs/guides/103_GUIDE_PORTABLE.MD` | exists |  |
| `docs/guides/004_GUIDE_MIGRATE_LEGACY_NUGET.MD` | `docs/guides/104_GUIDE_MIGRATE_LEGACY_NUGET.MD` | exists |  |
| `docs/migrations/001_HASHING_RENAME.MD` | `docs/migrations/101_HASHING_RENAME.MD` | exists |  |
| `docs/quality/001_CHECKLIST_PRODUCTION.MD` | `docs/quality/101_CHECKLIST_PRODUCTION.MD` | exists |  |
| `docs/references/001_REFERENCES_CORE.MD` | `docs/references/101_REFERENCES_CORE.MD` | exists |  |
| `docs/secure/001_HMAC_KEY_SETUP.MD` | `docs/secure/101_HMAC_KEY_SETUP.MD` | exists |  |
| `docs/security/010_CODEQL_DEFAULT_SETUP_GUARDRAIL.MD` | `docs/security/110_CODEQL_DEFAULT_SETUP_GUARDRAIL.MD` | exists |  |
| `docs/specs/001_SPEC_DIN.MD` | `docs/specs/101_SPEC_DIN.MD` | exists |  |
| `docs/verification/001_INDEX_TESTS.MD` | `docs/verification/101_INDEX_TESTS.MD` | exists |  |
| `docs/verification/002_FLOW_BDD.MD` | `docs/verification/102_FLOW_BDD.MD` | exists |  |
| `docs/verification/003_CATALOG_BDD.MD` | `docs/verification/103_CATALOG_BDD.MD` | exists |  |
| `docs/verification/004_MATRIX_HASHING.MD` | `docs/verification/104_MATRIX_HASHING.MD` | exists |  |
| `docs/versioning/001_POLICY_VERSIONING.MD` | `docs/versioning/101_POLICY_VERSIONING.MD` | exists |  |
| `docs/versioning/002_HISTORY_VERSIONS.MD` | `docs/versioning/102_HISTORY_VERSIONS.MD` | exists |  |
| `docs/versioning/003_CHANGELOG_RELEASES.MD` | `docs/versioning/103_CHANGELOG_RELEASES.MD` | exists |  |
| `docs/versioning/004_POLICY_LABELING.MD` | `docs/versioning/104_POLICY_LABELING.MD` | exists |  |

## 4. Checks (fail-closed)
(To be completed with concrete commands + outputs at the end.)

## 5. Edge Cases
- Numbers outside `0NN` (e.g. `230_*`): currently none detected (final verification pending).

## 6. Decision Log
- 2026-02-15: EN mirror documents initially created via 1:1 copy (translation + link fixes follow).
