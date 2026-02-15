# Code-Analyse-Methode

## 1. Ziel
Reproduzierbare File-Level- und Line-Level-Analyseartefakte erzeugen, ohne die public API zu aendern.

## 2. Machine Outputs (nur JSON)
Erzeugt durch `tools/audit/generate-code-analysis-json.sh`:
- `artifacts/audit/code_inventory.json`
- `artifacts/audit/callgraph_inventory.json`
- `artifacts/audit/dead_code_candidates.json`
- `artifacts/audit/redundancy_candidates.json`
- `artifacts/audit/hardening_candidates.json`
