<!-- LANG_SWITCH:BEGIN -->
[DE](002_AUDIT_CONTRACT_AND_GUARDRAILS.MD) | [EN](102_AUDIT_CONTRACT_AND_GUARDRAILS.MD)
<!-- LANG_SWITCH:END -->

# Audit-Vertrag und Guardrails

## 1. Zweck
Deterministische Evidence-Regeln fuer die Verifikation von Claims in `SECURITY.md`.

## 2. Statusmodell
Statuswerte im Result-Contract:
- `pass`: keine blockierenden Verstoesse.
- `fail`: mindestens ein blockierender Verstoss.
- `warn`: report-only Findings.

Operationales Tri-State-Mapping (Claim-Engine):
- `pass` -> schema `pass`
- `fail` -> schema `fail`
- `unknown` -> schema `warn` with explicit reason code in `rule_violations[].message`

## 3. Blocking-Policy
Blockierende Checks sind nur zulaessig, wenn sie deterministisch sind.
Live GitHub API Checks sind blocker-eligible mit strikten Anti-Flake-Kontrollen:
- 3 retries
- exponential backoff (2s, 4s, 8s)
- current implementation emits generic API failure messages after retries

Wenn alle Retries erschoepft sind:
- Blocker-Claims failen mit Reason-Code
- Report-only Claims emittieren `warn`

## 4. Evidence-Contract
Erforderliche Artefakte pro Check:
- `raw.log`
- `summary.md`
- `result.json` (muss `tools/ci/schema/result.schema.json` matchen)

## 5. Drift-Handling
- Policy-Drift ist fail-closed fuer Blocker-Claims.
- Unknown-States werden nie stillschweigend ignoriert.
- Jeder Failure muss einen Evidence-Pfad und ein reproduzierbares Kommando enthalten.
