# Versionslinie (Commit → Version)

Diese Tabelle wurde aus der Commit-Historie anhand der Policy in
`docs/versioning/001_POLICY_VERSIONING.MD` abgeleitet.
Heuristik fuer die Rueckwirkungs-Zuordnung:
- Major-Overrides: `5255724`, `fd03389`
- `feat:` oder `Add ...` (nicht-Doku) => Minor
- `docs|test|ci|chore|tooling|refactor|fix` => Patch

Aktueller Entwicklungsstand:
- Aktuelle Entwicklungslinie enthaelt `5.x` (aktuell veroeffentlichtes Tag: `v5.1.3`; naechstes vorgesehenes Tag: `v5.1.4`; Details in `docs/versioning/003_CHANGELOG_RELEASES.MD`).

| Version | Kurzbeschreibung | Commit | Keyword |
|---|---|---|---|
| `5.1.4` | Refactor-Cluster 7C abgeschlossen + Qodana-Alerts auf 0 (Archive-Infrastructure Qualifier-Cleanup, Tests) | [0a85c82](https://github.com/tomtastisch/FileClassifier/commit/0a85c82) | patch |
| `5.1.3` | PR-Governance-Haertung (DE-Naming, PR-Template, fail-closed Gate fuer `security/code-scanning/tools = 0`) | [0b488ac](https://github.com/tomtastisch/FileClassifier/commit/0b488ac) | patch |
| `5.1.2` | Gate4 Polling-Optimierung und Release-Haertung | [f12711d](https://github.com/tomtastisch/FileClassifier/commit/f12711d) | patch |
| `5.1.1` | Dependabot security-only mode und fail-closed Guards fuer secret-pflichtige Workflows | [d0ad8ec](https://github.com/tomtastisch/FileClassifier/commit/d0ad8ec) | patch |
| `5.1.0` | Security/Governance hardening wave: pinned actions, dependency review, labeler fixes, root assurance index | [e2a4a42](https://github.com/tomtastisch/FileClassifier/commit/e2a4a42) | minor |
| `5.0.0` | Finalize hashing API rename to EvidenceHashing and add optional HMAC digests | [444d027](https://github.com/tomtastisch/FileClassifier/commit/444d027) | breaking |
| `4.2.1` | Bump version to 4.2.1 for quality-gate hardening | [8ab274d](https://github.com/tomtastisch/FileClassifier/commit/8ab274d) | chore |
| `4.2.0` | docs: fix root readme parity, link sha targets, and preflight version gate | [9691bec](https://github.com/tomtastisch/FileClassifier/commit/9691bec) | docs |
| `4.1.3` | docs: deterministic restructure with SHA-locked links and policy-roc gate | [90a2825](https://github.com/tomtastisch/FileClassifier/commit/90a2825) | docs |
| `4.1.2` | chore(version): bump version for xunit v3 migration release | [d1ed2a9](https://github.com/tomtastisch/FileClassifier/commit/d1ed2a9) | chore |
| `4.1.1` | chore(version): bump patch version to satisfy CI guard | [d67050c](https://github.com/tomtastisch/FileClassifier/commit/d67050c) | chore |
| `4.1.0` | chore: update version to 4.1.0 and improve versioning logic | [a3dfe23](https://github.com/tomtastisch/FileClassifier/commit/a3dfe23) | chore |
| `4.0.0` | chore(versioning): bump baseline to 4.0.0 | [2a78f97](https://github.com/tomtastisch/FileClassifier/commit/2a78f97) | chore |
| `3.0.24` | docs: fix umlaut spellings | [22d40b9](https://github.com/tomtastisch/FileClassifier/commit/22d40b9) | docs |
| `3.0.23` | docs: normalize markdown language | [90310a0](https://github.com/tomtastisch/FileClassifier/commit/90310a0) | docs |
| `3.0.22` | docs: unify markdown structure and add maintenance checklists | [cb3341d](https://github.com/tomtastisch/FileClassifier/commit/cb3341d) | docs |
| `3.0.21` | docs(guides): unify structure and add step-by-step checklists with examples | [5f5c6ab](https://github.com/tomtastisch/FileClassifier/commit/5f5c6ab) | docs |
| `3.0.20` | docs: add guides for options and datatype extensions (#5) | [392c628](https://github.com/tomtastisch/FileClassifier/commit/392c628) | docs |
| `3.0.19` | docs(guides): add options and datatype extension playbooks | [1427e1e](https://github.com/tomtastisch/FileClassifier/commit/1427e1e) | docs |
| `3.0.18` | Refactor: deterministic hashing, archive hardening, and test/CI stabilization (#4) | [374732a](https://github.com/tomtastisch/FileClassifier/commit/374732a) | refactor |
| `3.0.17` | tooling(test): simplify readable output and strip technical test noise | [514922c](https://github.com/tomtastisch/FileClassifier/commit/514922c) | tooling |
| `3.0.16` | ci(docs): validate markdown heading anchors in link checker | [02884ff](https://github.com/tomtastisch/FileClassifier/commit/02884ff) | ci |
| `3.0.15` | ci(docs): add markdown link check gate | [cdbdfbd](https://github.com/tomtastisch/FileClassifier/commit/cdbdfbd) | ci |
| `3.0.14` | docs(structure): enforce readme coverage and update all abstraction references | [0d4cc5e](https://github.com/tomtastisch/FileClassifier/commit/0d4cc5e) | docs |
| `3.0.13` | docs(readme): add abstractions folder hierarchy graphic | [5853dd9](https://github.com/tomtastisch/FileClassifier/commit/5853dd9) | docs |
| `3.0.12` | docs(abstractions): finalize references after model folder split | [afdc592](https://github.com/tomtastisch/FileClassifier/commit/afdc592) | docs |
| `3.0.11` | refactor(abstractions): split models into detection archive hashing folders | [f25256f](https://github.com/tomtastisch/FileClassifier/commit/f25256f) | refactor |
| `3.0.10` | tooling(test): render bdd-readable output as clean per-test blocks | [268afe5](https://github.com/tomtastisch/FileClassifier/commit/268afe5) | tooling |
| `3.0.9` | test(core): add fail-closed internals coverage and raise ci gates | [27659c8](https://github.com/tomtastisch/FileClassifier/commit/27659c8) | test |
| `3.0.8` | refactor(tests): unify archive naming across archive-type tests | [093f372](https://github.com/tomtastisch/FileClassifier/commit/093f372) | refactor |
| `3.0.7` | docs(hash): add stage-by-stage deterministic test matrix | [c6dd754](https://github.com/tomtastisch/FileClassifier/commit/c6dd754) | docs |
| `3.0.6` | chore(version): introduce central deterministic project versioning | [b62ac79](https://github.com/tomtastisch/FileClassifier/commit/b62ac79) | chore |
| `3.0.5` | test(archive): table-drive archive hash and facade coverage | [1bc9b04](https://github.com/tomtastisch/FileClassifier/commit/1bc9b04) | test |
| `3.0.4` | refactor(test-docs): unify archive naming and add glossary | [c412613](https://github.com/tomtastisch/FileClassifier/commit/c412613) | refactor |
| `3.0.3` | test(e2e): erweitere hash-invarianz fuer archive positiv und negativ | [12dbe32](https://github.com/tomtastisch/FileClassifier/commit/12dbe32) | test |
| `3.0.2` | test(hash): prove archive extract-materialize hash invariance | [48c3f8b](https://github.com/tomtastisch/FileClassifier/commit/48c3f8b) | test |
| `3.0.1` | docs: vereinheitliche zip-terminologie auf archiv-kontext | [33eac47](https://github.com/tomtastisch/FileClassifier/commit/33eac47) | docs |
| `3.0.0` | refactor(config): centralize project options and hashing defaults | [fd03389](https://github.com/tomtastisch/FileClassifier/commit/fd03389) | breaking |
| `2.0.3` | refactor: vereinheitliche archive reason-codes und detector flow | [254ea22](https://github.com/tomtastisch/FileClassifier/commit/254ea22) | refactor |
| `2.0.2` | refactor: konsolidiere archive-entry flow und haerte hash optionen | [d888f35](https://github.com/tomtastisch/FileClassifier/commit/d888f35) | refactor |
| `2.0.1` | docs: korrigiere reason-code referenzen fuer archive naming | [cffbb4b](https://github.com/tomtastisch/FileClassifier/commit/cffbb4b) | docs |
| `2.0.0` | refactor: vereinheitliche zip-benennungen auf archive | [5255724](https://github.com/tomtastisch/FileClassifier/commit/5255724) | breaking |
| `1.6.2` | test(hash): assert physical equals logical for plain files | [9c2749c](https://github.com/tomtastisch/FileClassifier/commit/9c2749c) | test |
| `1.6.1` | test(ci): freeze hashing api contract and enforce coverage gates | [84b1d8f](https://github.com/tomtastisch/FileClassifier/commit/84b1d8f) | test |
| `1.6.0` | feat(hash): add deterministic hashing api, evidence models, and roundtrip tests | [56aa4af](https://github.com/tomtastisch/FileClassifier/commit/56aa4af) | feature |
| `1.5.3` | Codex/full refactor baseline (#3) | [a2e88cd](https://github.com/tomtastisch/FileClassifier/commit/a2e88cd) | maintenance |
| `1.5.2` | tests: make coverage loops explicitly filtered | [f73c24e](https://github.com/tomtastisch/FileClassifier/commit/f73c24e) | maintenance |
| `1.5.1` | Update src/FileTypeDetection/Infrastructure/ArchiveInternals.vb | [0f6116a](https://github.com/tomtastisch/FileClassifier/commit/0f6116a) | maintenance |
| `1.5.0` | feat: add DeterministicHashEvidence class for hash calculations | [4179164](https://github.com/tomtastisch/FileClassifier/commit/4179164) | feature |
| `1.4.2` | refactor(tests): use disposable temp scope for deterministic cleanup | [fa201eb](https://github.com/tomtastisch/FileClassifier/commit/fa201eb) | refactor |
| `1.4.1` | refactor: normalize option snapshots and archive validation flow | [9378f50](https://github.com/tomtastisch/FileClassifier/commit/9378f50) | refactor |
| `1.4.0` | feat: add unified archive backend and update test documentation | [99a7405](https://github.com/tomtastisch/FileClassifier/commit/99a7405) | feature |
| `1.3.19` | docs: link production readiness checklist in indexes | [8c14563](https://github.com/tomtastisch/FileClassifier/commit/8c14563) | docs |
| `1.3.18` | docs: add production readiness checklist | [b5edeef](https://github.com/tomtastisch/FileClassifier/commit/b5edeef) | docs |
| `1.3.17` | test(bdd): reorganize features, tags, and cleanup infrastructure | [feb4bbc](https://github.com/tomtastisch/FileClassifier/commit/feb4bbc) | test |
| `1.3.16` | docs: split API docs and align README cross-links (#2) | [4c17a21](https://github.com/tomtastisch/FileClassifier/commit/4c17a21) | docs |
| `1.3.15` | refactor(detector): remove closure-based stream delegates and clean API docs typos | [edd0741](https://github.com/tomtastisch/FileClassifier/commit/edd0741) | refactor |
| `1.3.14` | docs: professionalize final documentation and wording | [a91d82c](https://github.com/tomtastisch/FileClassifier/commit/a91d82c) | docs |
| `1.3.13` | docs: rename all module index files to README for GitHub folder views | [af6a5ef](https://github.com/tomtastisch/FileClassifier/commit/af6a5ef) | docs |
| `1.3.12` | docs(license): add MIT license for free use and modification | [0b6c367](https://github.com/tomtastisch/FileClassifier/commit/0b6c367) | docs |
| `1.3.11` | ci: add GitHub Actions build-and-test workflow | [1460089](https://github.com/tomtastisch/FileClassifier/commit/1460089) | ci |
| `1.3.10` | docs(index): align architecture flow direction | [8ae8441](https://github.com/tomtastisch/FileClassifier/commit/8ae8441) | docs |
| `1.3.9` | chore(repo): exclude portable mirror and harden publish set | [a4dff0b](https://github.com/tomtastisch/FileClassifier/commit/a4dff0b) | chore |
| `1.3.8` | docs: vereinheitliche API-Doku, Diagramme und Dependency-Nachweise | [042cdd7](https://github.com/tomtastisch/FileClassifier/commit/042cdd7) | docs |
| `1.3.7` | test(bdd): tag materializer scenarios and mirror negative paths in unit tests | [c8e265c](https://github.com/tomtastisch/FileClassifier/commit/c8e265c) | test |
| `1.3.6` | test(bdd): add materializer negative integration scenarios | [4715b2a](https://github.com/tomtastisch/FileClassifier/commit/4715b2a) | test |
| `1.3.5` | test(bdd): add zip extract->bytes->materialize integration chains | [b26542b](https://github.com/tomtastisch/FileClassifier/commit/b26542b) | test |
| `1.3.4` | chore(cleanup): remove obsolete phase artifact documents | [4ace8df](https://github.com/tomtastisch/FileClassifier/commit/4ace8df) | chore |
| `1.3.3` | refactor(options): support headerOnlyNonZip in JSON snapshot flow | [ae5f1eb](https://github.com/tomtastisch/FileClassifier/commit/ae5f1eb) | refactor |
| `1.3.2` | refactor(io): centralize stream buffer and sniff defaults | [1c9051f](https://github.com/tomtastisch/FileClassifier/commit/1c9051f) | refactor |
| `1.3.1` | refactor(zip): enforce byte-only disk writes and harden nested zip detection | [c52838c](https://github.com/tomtastisch/FileClassifier/commit/c52838c) | refactor |
| `1.3.0` | feat: add file type detection script and validation utility | [77727aa](https://github.com/tomtastisch/FileClassifier/commit/77727aa) | feature |
| `1.2.7` | #!/usr/bin/env bash set -euo pipefail | [8a7d36a](https://github.com/tomtastisch/FileClassifier/commit/8a7d36a) | maintenance |
| `1.2.6` | refactor: centralize destination path guards for extraction and materialization | [563e491](https://github.com/tomtastisch/FileClassifier/commit/563e491) | refactor |
| `1.2.5` | refactor: share zip payload guards and enforce overwrite invariants | [931f1f0](https://github.com/tomtastisch/FileClassifier/commit/931f1f0) | refactor |
| `1.2.4` | refactor: simplify detector reason handling and add property invariants | [1e87af1](https://github.com/tomtastisch/FileClassifier/commit/1e87af1) | refactor |
| `1.2.3` | refactor: harden materialization and option validation paths | [9bfef95](https://github.com/tomtastisch/FileClassifier/commit/9bfef95) | refactor |
| `1.2.2` | docs: add DIN-oriented spec and consolidate portable sync tooling | [7808e36](https://github.com/tomtastisch/FileClassifier/commit/7808e36) | docs |
| `1.2.1` | fix: restore portable mirror sources after unintended deletion | [e3727f7](https://github.com/tomtastisch/FileClassifier/commit/e3727f7) | fix |
| `1.2.0` | feat: add file materializer and central options facade | [01dcc07](https://github.com/tomtastisch/FileClassifier/commit/01dcc07) | feature |
| `1.1.8` | Align FileTypeDetection layout and enforce 1:1 portable sync | [bf1fe23](https://github.com/tomtastisch/FileClassifier/commit/bf1fe23) | maintenance |
| `1.1.7` | Refactor FileTypeDetection structure, docs, and deterministic sync | [991401a](https://github.com/tomtastisch/FileClassifier/commit/991401a) | refactor |
| `1.1.6` | docs: add comprehensive reports on code health, dependencies, and operability | [1c6c101](https://github.com/tomtastisch/FileClassifier/commit/1c6c101) | docs |
| `1.1.5` | # C0) CODE_HEALTH_REPORT.md | [7de19a3](https://github.com/tomtastisch/FileClassifier/commit/7de19a3) | maintenance |
| `1.1.4` | # A0) BASELINE.md | [8625546](https://github.com/tomtastisch/FileClassifier/commit/8625546) | maintenance |
| `1.1.3` | docs(portable): add copy/paste package, API guide, and normalized structure | [a997a91](https://github.com/tomtastisch/FileClassifier/commit/a997a91) | docs |
| `1.1.2` | chore: remove .bak artifacts and ignore backup files | [eadb35e](https://github.com/tomtastisch/FileClassifier/commit/eadb35e) | chore |
| `1.1.1` | Remove redundant solution file | [9158395](https://github.com/tomtastisch/FileClassifier/commit/9158395) | maintenance |
| `1.1.0` | Add extension verification to FileTypeDetector and corresponding tests | [80e74ce](https://github.com/tomtastisch/FileClassifier/commit/80e74ce) | feature |
| `1.0.0` | Add initial implementation of FileTypeDetection library and tests | [d9a6015](https://github.com/tomtastisch/FileClassifier/commit/d9a6015) | initial |
