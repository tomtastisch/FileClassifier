<!-- LANG_SWITCH:BEGIN -->
[DE](015_DOC_BILINGUAL_MAPPING.MD) | [EN](115_DOC_BILINGUAL_MAPPING.MD)
<!-- LANG_SWITCH:END -->

# Bilinguale Dokumentation: DE<->EN Mapping (0NN_ -> 1NN_)

## 1. Ziel & Scope
Dieser Report dokumentiert deterministisch das Mapping aller Dokumente nach dem Schema `NNN_*` (DE ist Primary) auf ihre englischen Spiegeldateien `1NN_*`.

Scope:
- Betrifft alle Dokumente mit Dateinamen `NNN_<slug>.md` oder `NNN_<slug>.MD` (unabhaengig vom Ordner).
- EN ist semantisch aequivalent zur DE-Version (gleiche Struktur/Abschnitte; Codebloecke/Commands identisch).

Nicht-Ziele:
- Keine Code-/Build-/CI-Aenderungen (ausser Markdown-Linkfixes innerhalb von `.md/.MD`).

## 2. Namensregeln (SSOT)
- DE-Datei: `0NN_<slug>.<ext>` mit dreistelliger Nummer `0NN` (z. B. `030_...`).
- EN-Datei: `1NN_<slug>.<ext>`.
- `NN` = letzte zwei Ziffern der DE-Nummer.
- `slug` und Extension bleiben identisch (deterministisches Mapping).

Mapping-Funktion:
- `0NN_*` -> `1NN_*` (gleicher Ordner, gleicher `slug`, gleiche Extension).

## 3. Inventory + Mapping-Tabelle
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
| `docs/audit/015_DOC_BILINGUAL_MAPPING.MD` | `docs/audit/115_DOC_BILINGUAL_MAPPING.MD` | exists | Mapping-Report-Dateipaar (DE->EN). |
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
Alle Checks sind vom Repo-Root ausfuehrbar.

### 4.1 Inventory-Listen (sorted)
Kommandos:
```bash
find . -type f \( -name '0??_*.md' -o -name '0??_*.MD' \) -print | sed 's|^\./||' | sort > /tmp/docs_0nn.txt
find . -type f \( -name '1??_*.md' -o -name '1??_*.MD' \) -print | sed 's|^\./||' | sort > /tmp/docs_1nn.txt
```

Output (0NN):
```text
docs/001_INDEX_CORE.MD
docs/010_API_CORE.MD
docs/020_ARCH_CORE.MD
docs/021_USAGE_NUGET.MD
docs/audit/000_HASHING_BASELINE.MD
docs/audit/000_INDEX.MD
docs/audit/002_AUDIT_CONTRACT_AND_GUARDRAILS.MD
docs/audit/003_SECURITY_ASSERTION_TRACEABILITY.MD
docs/audit/004_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD
docs/audit/005_CODE_ANALYSIS_METHOD.MD
docs/audit/006_CODE_REVIEW_FINDINGS.MD
docs/audit/007_THREAT_MODEL.MD
docs/audit/008_INCIDENT_RESPONSE_RUNBOOK.MD
docs/audit/009_SUPPLY_CHAIN_BASELINE.MD
docs/audit/010_REFACTOR_BACKLOG.MD
docs/audit/011_SECURITY_BENCHMARK.MD
docs/audit/012_WAVE_EXECUTION_DOD.MD
docs/audit/013_SCORECARD_GOVERNANCE_ALERT_MAPPING.MD
docs/audit/014_EVIDENCE_REPORT_ISSUE_67.MD
docs/audit/015_DOC_BILINGUAL_MAPPING.MD
docs/ci/001_PIPELINE_CI.MD
docs/ci/002_NUGET_TRUSTED_PUBLISHING.MD
docs/contracts/001_CONTRACT_HASHING.MD
docs/governance/001_POLICY_CI.MD
docs/governance/002_POLICY_LABELING.MD
docs/governance/002_POLICY_NAMING_UNIFIED.MD
docs/governance/003_INDEX_GOVERNANCE.MD
docs/governance/003_POLICY_VERSIONING_SVT.MD
docs/governance/004_POLICY_DOCUMENTATION.MD
docs/governance/005_POLICY_NAMING.MD
docs/governance/006_INDEX_CI_RULES.MD
docs/governance/007_POLICY_BRANCH_PR_NAMING_DE.MD
docs/guides/000_INDEX_GUIDES.MD
docs/guides/001_GUIDE_OPTIONS.MD
docs/guides/002_GUIDE_DATATYPE.MD
docs/guides/003_GUIDE_PORTABLE.MD
docs/guides/004_GUIDE_MIGRATE_LEGACY_NUGET.MD
docs/migrations/001_HASHING_RENAME.MD
docs/quality/001_CHECKLIST_PRODUCTION.MD
docs/references/001_REFERENCES_CORE.MD
docs/secure/001_HMAC_KEY_SETUP.MD
docs/security/010_CODEQL_DEFAULT_SETUP_GUARDRAIL.MD
docs/specs/001_SPEC_DIN.MD
docs/verification/001_INDEX_TESTS.MD
docs/verification/002_FLOW_BDD.MD
docs/verification/003_CATALOG_BDD.MD
docs/verification/004_MATRIX_HASHING.MD
docs/versioning/001_POLICY_VERSIONING.MD
docs/versioning/002_HISTORY_VERSIONS.MD
docs/versioning/003_CHANGELOG_RELEASES.MD
docs/versioning/004_POLICY_LABELING.MD
```

Output (1NN):
```text
docs/101_INDEX_CORE.MD
docs/110_API_CORE.MD
docs/120_ARCH_CORE.MD
docs/121_USAGE_NUGET.MD
docs/audit/100_HASHING_BASELINE.MD
docs/audit/100_INDEX.MD
docs/audit/102_AUDIT_CONTRACT_AND_GUARDRAILS.MD
docs/audit/103_SECURITY_ASSERTION_TRACEABILITY.MD
docs/audit/104_CERTIFICATION_AND_ATTESTATION_ROADMAP.MD
docs/audit/105_CODE_ANALYSIS_METHOD.MD
docs/audit/106_CODE_REVIEW_FINDINGS.MD
docs/audit/107_THREAT_MODEL.MD
docs/audit/108_INCIDENT_RESPONSE_RUNBOOK.MD
docs/audit/109_SUPPLY_CHAIN_BASELINE.MD
docs/audit/110_REFACTOR_BACKLOG.MD
docs/audit/111_SECURITY_BENCHMARK.MD
docs/audit/112_WAVE_EXECUTION_DOD.MD
docs/audit/113_SCORECARD_GOVERNANCE_ALERT_MAPPING.MD
docs/audit/114_EVIDENCE_REPORT_ISSUE_67.MD
docs/audit/115_DOC_BILINGUAL_MAPPING.MD
docs/ci/101_PIPELINE_CI.MD
docs/ci/102_NUGET_TRUSTED_PUBLISHING.MD
docs/contracts/101_CONTRACT_HASHING.MD
docs/governance/101_POLICY_CI.MD
docs/governance/102_POLICY_LABELING.MD
docs/governance/102_POLICY_NAMING_UNIFIED.MD
docs/governance/103_INDEX_GOVERNANCE.MD
docs/governance/103_POLICY_VERSIONING_SVT.MD
docs/governance/104_POLICY_DOCUMENTATION.MD
docs/governance/105_POLICY_NAMING.MD
docs/governance/106_INDEX_CI_RULES.MD
docs/governance/107_POLICY_BRANCH_PR_NAMING_DE.MD
docs/guides/100_INDEX_GUIDES.MD
docs/guides/101_GUIDE_OPTIONS.MD
docs/guides/102_GUIDE_DATATYPE.MD
docs/guides/103_GUIDE_PORTABLE.MD
docs/guides/104_GUIDE_MIGRATE_LEGACY_NUGET.MD
docs/migrations/101_HASHING_RENAME.MD
docs/quality/101_CHECKLIST_PRODUCTION.MD
docs/references/101_REFERENCES_CORE.MD
docs/secure/101_HMAC_KEY_SETUP.MD
docs/security/110_CODEQL_DEFAULT_SETUP_GUARDRAIL.MD
docs/specs/101_SPEC_DIN.MD
docs/verification/101_INDEX_TESTS.MD
docs/verification/102_FLOW_BDD.MD
docs/verification/103_CATALOG_BDD.MD
docs/verification/104_MATRIX_HASHING.MD
docs/versioning/101_POLICY_VERSIONING.MD
docs/versioning/102_HISTORY_VERSIONS.MD
docs/versioning/103_CHANGELOG_RELEASES.MD
docs/versioning/104_POLICY_LABELING.MD
```

### 4.2 Zaehlung + Diff-Listen (missing/orphan/collision)
Kommandos:
```bash
sed 's|/0\\([0-9][0-9]\\)_|/1\\1_|' /tmp/docs_0nn.txt | sort > /tmp/docs_expected_1nn.txt
echo "count_0 $(wc -l < /tmp/docs_0nn.txt)"
echo "count_1 $(wc -l < /tmp/docs_1nn.txt)"
echo "count_expected_1 $(wc -l < /tmp/docs_expected_1nn.txt)"
echo 'missing_en:'; comm -23 /tmp/docs_expected_1nn.txt /tmp/docs_1nn.txt
echo 'orphan_en:'; comm -13 /tmp/docs_expected_1nn.txt /tmp/docs_1nn.txt
echo 'mapping_collisions:'; sort /tmp/docs_expected_1nn.txt | uniq -d
```

Output:
```text
count_0 51
count_1 51
count_expected_1 51
missing_en:
orphan_en:
mapping_collisions:
```

### 4.3 Keine NNN_* ausserhalb 0NN/1NN
Kommandos:
```bash
find . -type f \( -name '*.md' -o -name '*.MD' \) -name '[0-9][0-9][0-9]_*' -print \
  | sed 's|^\./||' | sort \
  | rg -v '(^|/)0[0-9]{2}_.+\\.(md|MD)$|(^|/)1[0-9]{2}_.+\\.(md|MD)$'
```

Output:
```text
```

### 4.4 Link-Konsistenz (EN verweist nicht auf 0NN)
Kommandos:
```bash
rg -n --glob 'docs/**/1??_*.M{D,d}' -S 'docs/0[0-9]{2}_' docs
```

Output:
```text
```

### 4.5 Repo-Checks (Doku)
Kommandos:
```bash
python3 tools/check-docs.py
python3 tools/check-doc-consistency.py
python3 tools/check-doc-shell-compat.py
```

Output:
```text
Doc check OK
Doc consistency check OK
Doc shell compatibility check OK
```

### 4.6 Guardrail: Keine Nicht-Markdown-Diffs
Kommandos:
```bash
git diff --name-only main...HEAD | rg -v '\\.(md|MD)$'
```

Output:
```text
```

## 5. Edge Cases
- Nummern ausserhalb `0NN` (z. B. `230_*`): keine gefunden (siehe Check 4.3).
- `docs/verification/103_CATALOG_BDD.MD`: enthält kanonische deutsche Satzmuster (Gherkin) als SSOT; EN-Dokument erlaeutert die Patterns, aber die Pattern-Texte selbst bleiben absichtlich deutsch.
- `docs/versioning/102_HISTORY_VERSIONS.MD`: Spalte "Short description" wird fuer deterministische Traceability bewusst nicht auf eine Sprache normalisiert (Original-Commit/PR-Intent).
- `docs/audit/103_SECURITY_ASSERTION_TRACEABILITY.MD`: Spalte "SECURITY Anchor" nutzt die originalen deutschen Ueberschriften aus der eingefrorenen `SECURITY.md` als SSOT.

## 6. Decision Log
- 2026-02-15: EN-Spiegeldateien initial durch 1:1 Copy angelegt (Translation + Link-Fixes folgen).
- 2026-02-15: Repo-weite Link-Ausrichtung in EN-Dokumenten (EN soll primaer EN referenzieren), ohne Aenderung von Codebloecken/CLI-Beispielen.
- 2026-02-15: `docs/120_ARCH_CORE.MD` vollstaendig ins Englische ueberfuehrt, Struktur identisch zu `docs/020_ARCH_CORE.MD`.
