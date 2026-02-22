<!-- LANG_SWITCH:BEGIN -->
[DE](005_CODE_ANALYSIS_METHOD.MD) | [EN](105_CODE_ANALYSIS_METHOD.MD)
<!-- LANG_SWITCH:END -->

# Code-Analyse-Methode

## 1. Ziel
Reproduzierbare File-Level- und Line-Level-Analyseartefakte erzeugen, ohne die public API zu ändern.

## 2. Machine Outputs (nur JSON)
Erzeugt durch `tools/audit/generate-code-analysis-json.sh`:
- `artifacts/audit/code_inventory.json`
- `artifacts/audit/callgraph_inventory.json`
- `artifacts/audit/dead_code_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `artifacts/audit/hardening_candidates.json`
