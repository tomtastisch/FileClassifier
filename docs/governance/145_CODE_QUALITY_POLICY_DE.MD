<!-- LANG_SWITCH:BEGIN -->
[DE](045_CODE_QUALITY_POLICY_DE.MD) | [EN](145_CODE_QUALITY_POLICY_DE.MD)
<!-- LANG_SWITCH:END -->

# Code Quality & Documentation Policy (EN)
Status: binding (project policy)
Scope: `src/FileTypeDetection/*` (VB.NET and C#), public API + internal implementation

## 1. Purpose
This policy defines a consistent, auditable schema for:
- code structure (readability, semantics, member ordering)
- error handling (fail-closed, consistent logging)
- documentation (German for public API, complete, with real umlauts)
- reproducible Codex changes without behavioral drift

## 2. Normative Orientation (DIN/ISO/IEC/IEEE)
Note: there is no single “DIN for code formatting”. This is an internal rule set aligned with established standards for information quality and software quality:
- ISO/IEC/IEEE 26514:2022-01
- ISO/IEC 25010:2023
- ISO/IEC/IEEE 15289:2019
- optional: DIN EN IEC/IEEE 82079-1:2021-09

## 3. Hard Principles
- Never transliterate German umlauts: `ä ö ü Ä Ö Ü ß` (no `ae/oe/ue/ss`).
- Files remain UTF-8.
- No behavior changes through formatting/docs:
  - no signature changes
  - no semantic changes in exceptions/returns/side effects
  - no new external dependencies

## 4. Shared vs. Instance (Construction Rule)
### 4.1 Shared (utility) is allowed only if all are true
- type is stateless
- no DI or swappable dependencies needed
- no polymorphism/test doubles via interfaces needed
- typically: `NotInheritable` + `Private Sub New()` + only shared members

### 4.2 Instance/service type is mandatory if at least one applies
- dependencies should be injectable/swappable (I/O, logger, policy, clock, provider, ...)
- testability via DI/interfaces is required
- per-instance state/configuration is meaningful

Within style/docs-only updates, type kind must not be changed unless already clearly established as utility/service.

## 5. File and Type Structure
### 5.1 File order
1) file header comment (policy/context block, not XML docs)
2) `Option Strict On` / `Option Explicit On` (VB)
3) imports
4) namespace
5) types

### 5.2 Type layout (member order)
1) XML docs on type (`summary` + `remarks`)
2) constants (`Const`) / `Shared ReadOnly`
3) fields/properties (only when type state exists)
4) constructors
5) public API (all public members, ordered)
6) internal/private helpers
7) nested types (only if needed; at end)

### 5.3 Semantic blocks in methods
Use visible block separation (empty line + block comment markers):
- options/snapshot
- guard clauses (fail-closed)
- normalization/canonicalization
- branches (for example secure extraction)
- fallback
- I/O helpers separated

## 6. Variable Rule
- All local variables are declared in a declaration block at the start of the function.
- Placement:
  - function-local when only needed there
  - class-level only when shared across members by design
- Alignment: column-style (`Dim` / name / `As` / initializer) using tabs/spaces.
- Exception: `Using`/`For` headers may contain local variables when idiomatic and more readable.

## 6.1 Naming Conventions (VB.NET + C#)
- Public API is a contract and must be convention-compliant.
- No umlauts in identifiers.
- No underscores in public API names (except required interop contracts).
- Avoid unclear abbreviations in public API.
- Keep PascalCase for namespaces/types/public members.
- Parameters/locals: camelCase.
- Private fields: `_camelCase` (or existing repo SSOT if different).
- Boolean names should be positive and use `is/has/can/should` where applicable.
- `Async` suffix only for true async semantics.

## 7. Exception Handling & Logging (Fail-Closed)
- Prefer catch filter form in VB: `Catch ex As Exception When TypeOf ex Is ...`
- Forbidden redundant pseudo-filter: `Catch ex As Exception When TypeOf ex Is Exception`
  If all exceptions must be handled, use `Catch ex As Exception` instead.
- No unfiltered catch-all unless technically required and documented.
- No silent swallow: log (`Warn`/`Error`) and preserve prior fail-closed behavior.
- Keep log wording consistent (German with proper umlauts in this project).
- No exception semantics change (no new wrapping, no new throws).

## 8. Documentation Standard (Public API)
### 8.1 Language/characters
- German language for public API docs.
- Real umlauts, no transliteration.

### 8.2 Minimum per public type/member
- Type:
  - `<summary>`: 1-3 sentences (purpose/scope)
  - `<remarks>`: structured sections (purpose, responsibilities, non-goals, side effects, error cases, security notes, threading if relevant)
- Public methods/functions:
  - `<summary>`, all `<param>`, always `<returns>`
  - `<exception>` only if actually propagated by API contract; otherwise describe fail-closed behavior in remarks
  - `<example>` when useful

### 8.3 “DIN-/standard-oriented” means
- precise wording
- consistent terminology
- explicit error paths and side effects

## 9. Definition of Done (DoD)
For each file in `src/FileTypeDetection/*`:
- layout complies with sections 5-7
- public API documented per section 8
- umlauts preserved (no `ae/oe/ue` transliteration)
- build/tests successful
- no public signature changes
- no behavior drift (no new dependencies, no semantic changes)

## 10. Style Reference (SSOT)
The authoritative style/semantics template is the reference block in `docs/governance/045_CODE_QUALITY_POLICY_DE.MD` section “10. STYLE REFERENCE (SSOT)”.
Apply that schema to all files in `src/FileTypeDetection/*`.
Deviations are allowed only when technically required and must be justified in diff summary.

## Source of Truth Note
This English edition is provided for accessibility. If wording differs, the German policy file
`docs/governance/045_CODE_QUALITY_POLICY_DE.MD` is authoritative.
