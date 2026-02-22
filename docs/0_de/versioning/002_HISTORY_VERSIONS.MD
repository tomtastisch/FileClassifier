<!-- LANG_SWITCH:BEGIN -->
[DE](002_HISTORY_VERSIONS.MD) | [EN](../../1_en/versioning/002_HISTORY_VERSIONS.MD)
<!-- LANG_SWITCH:END -->

# Versionslinie (Commit → Version)

Diese Tabelle wurde aus der Commit-Historie anhand der Policy in
`docs/versioning/001_POLICY_VERSIONING.MD` abgeleitet.
Heuristik für die Rückwirkungs-Zuordnung:
- Major-Overrides: `5255724`, `fd03389`
- `feat:` oder `Add ...` (nicht-Doku) => Minor
- `docs|test|ci|chore|tooling|refactor|fix` => Patch

Aktueller Entwicklungsstand:
- Aktuelle Entwicklungslinie enthält `6.x` (aktueller Arbeitsstand: `v6.1.14`; Details in `docs/versioning/003_CHANGELOG_RELEASES.MD`).

Hinweis:
- Die Spalte `Keyword` verwendet den technischen Klassifizierungswert aus der Historie.
- Einzelne Committitel bleiben in der Originalsprache, wenn sie als exakter Quelltextnachweis übernommen wurden.

| Version | Kurzbeschreibung | Commit | Keyword |
|---|---|---|---|
| `6.1.14` | 6.1.14 Pipeline-Konvergenz geschlossen: Release-Workflow erzwingt NuGet-Online-Konvergenz jetzt fail-closed auch für `workflow_dispatch`, Release-Metadaten werden artefaktbasiert deterministisch aufgelöst und Fuzzing-Blocker-/Governance-Evidence-Dokumentation entsprechend nachgezogen | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.13` | FC-0016 abgeschlossen: In-Code-XML-Dokumentation im gesamten CSCore sprachlich auf Deutsch vereinheitlicht (inkl. konsistenter `<b>`- und `<br/>`-Verwendungsstruktur) und Terminologie für Audit-/Betriebskontexte konsolidiert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.12` | FC-0015 abgeschlossen: Bilinguale Doku-Rasterstruktur in `docs/0_de` und `docs/1_en` mit identischer Dateimenge und verpflichtenden Language-Switch-Headern kanonisch eingeführt; Sync-Tooling fail-closed gehärtet (Parity/Switch-Checks + Stale-Pruning) und PR-Scope-Allowlist für die neue Struktur erweitert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.11` | FC-0014 abgeschlossen: Archive-/Path-Policy-Kernlogik (relative Archivpfad-Normalisierung und Root-Path-Policy) in CSCore zentralisiert (`ArchivePathPolicyUtility`) und via Bridge produktiv in `ArchiveEntryPathPolicy`/`DestinationPathGuard` genutzt; Telemetrie-/Contract-Abdeckung erweitert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.10` | FC-0013 abgeschlossen: Hash-Evidence-Policy (Label/Notes/HMAC-Env-Key) in CSCore zentralisiert (`EvidencePolicyUtility`), Bridge-Delegation im `EvidenceHashingCore` produktiv aktiviert und Contracts/Telemetry entsprechend erweitert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.9` | FC-0012 abgeschlossen: OpenXML/OpenDocument- und Legacy-Office-Entscheidungslogik weiter in CSCore ausgelagert (`OfficePolicyUtility`) und via Bridge in `CoreInternals` produktiv genutzt; Telemetrie-/Regressionstests sowie Referenzdokumentation entsprechend erweitert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.8` | FC-0011 abgeschlossen: DetectDetailed-Endungs- und ReasonCode-Policy weiter in CSCore ausgelagert (inkl. produktiver Nutzung von `DetectionSignal`/`DetectionSummary` via Mapperly), Bridge-Telemetrie erweitert und vollständige CSCore-Moduldokumentation (Root/Model/Mapping/Utilities) nachgezogen | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.7` | FC-0010 abgeschlossen: Analyzer-Major-Upgrade (`Meziantou 3.0.12`, `NetAnalyzers 10.0.103`), netstandard2.0-Dependency-Strategie via direktes CodePages-Pinning + dokumentiertem Microsoft.NETCore.Platforms-Meta-Ausnahmefall sowie end-to-end run.sh-Checks grün verifiziert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.6` | FC-0009 abgeschlossen: PolySharp-basierter `IsExternalInit`-Polyfill eingeführt, `DotNet.ReproducibleBuilds` auf `2.0.2` angehoben und fail-closed Banned-API-Gate auf VB-Core inkl. unsicherer ZIP-Extraktions-APIs erweitert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.5` | FC-0008 abgeschlossen: Reproducible-Build-Basis als zentrales Paket eingeführt und damit Build-/Provenance-Auditierbarkeit systemweit gehärtet | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.4` | FC-0007 abgeschlossen: Analyzer-/Banned-API-Baseline auf alle relevanten C#-Projekte erweitert und Options-Normalisierung via CSCore-Record+Mapperly Utility zentralisiert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.3` | FC-0006 abgeschlossen: Hash-/Materializer-Logik weiter in CSCore ausgelagert (inkl. fail-closed Telemetry/Audit-Snapshot und deterministischem Decision-Matrix-Regressionstest) | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.2` | FC-0005 gestartet: CSCore-Utility-Offloading für Enum/Iterable/Argument- und Exception-Filter-Gates via fail-closed Runtime-Bridge und package-backed Contract-Härtung | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.1` | FC-0004 abgeschlossen: Records/Mapperly-Pilot in CSCore mit fail-closed CI-/Governance-Nachweisen und netstandard-Compat (`IsExternalInit`) | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.1.0` | FC-0003 abgeschlossen: Single-Package-Integration von CSCore inkl. Packaging-/Consumer-Gate und transitive Runtime-Verifikation | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | minor |
| `6.0.2` | FC-0002 abgeschlossen: CSCore-TFM/SDK-Kompatibilität festgelegt, dokumentiert und gegen fail-closed Build-Checks verifiziert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.0.1` | FC-0001 abgeschlossen: CSCore-Unterprojekt eingeführt und deterministische Build-Baseline für Cross-Language-Single-Package gesetzt | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/main...HEAD) | patch |
| `6.0.0` | Breaking-Release: `FileKind`-Enum und `HashRoundTripReport`-Public-API auf neues Slot-/Methodenmodell umgestellt, Hashing-Interna in Core/RoundTrip/Io ausgelagert | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/v5.2.1...HEAD) | breaking |
| `5.2.1` | SharpCompress-API auf typsichere Aufrufe umgestellt, tar.gz-Verarbeitung fail-closed gehärtet und Qodana-CI-Gate als Pflichtlauf dokumentiert/erzwungen | [unreleased](https://github.com/tomtastisch/FileClassifier/compare/v5.2.0...HEAD) | patch |
| `5.2.0` | netstandard2.0-Compat-Layer eingeführt, Provider-Struktur konsolidiert und TFM-Multi-Targeting erweitert | [8d65a52](https://github.com/tomtastisch/FileClassifier/commit/8d65a52) | minor |
| `5.1.4` | Refactor-Cluster 7C abgeschlossen + Qodana-Alerts auf 0 + Version-Bump für Release | [2adeb83](https://github.com/tomtastisch/FileClassifier/commit/2adeb83) | patch |
| `5.1.3` | PR-Governance-Härtung (DE-Naming, PR-Template, fail-closed Gate für `security/code-scanning/tools = 0`) | [0b488ac](https://github.com/tomtastisch/FileClassifier/commit/0b488ac) | patch |
| `5.1.2` | Gate4 Polling-Optimierung und Release-Härtung | [f12711d](https://github.com/tomtastisch/FileClassifier/commit/f12711d) | patch |
| `5.1.1` | Dependabot security-only mode und fail-closed Guards für secret-pflichtige Workflows | [d0ad8ec](https://github.com/tomtastisch/FileClassifier/commit/d0ad8ec) | patch |
| `5.1.0` | Security-/Governance-Härtungswelle: gepinnte Actions, Dependency-Review, Labeler-Fixes, Root-Assurance-Index | [e2a4a42](https://github.com/tomtastisch/FileClassifier/commit/e2a4a42) | minor |
| `5.0.0` | Hashing-API-Rename auf `EvidenceHashing` finalisiert und optionale HMAC-Digests hinzugefügt | [444d027](https://github.com/tomtastisch/FileClassifier/commit/444d027) | breaking |
| `4.2.1` | Version auf 4.2.1 für Quality-Gate-Härtung angehoben | [8ab274d](https://github.com/tomtastisch/FileClassifier/commit/8ab274d) | chore |
| `4.2.0` | Dokumentation: Root-README-Parität korrigiert, SHA-Links angepasst und Preflight-Version-Gate härter gemacht | [9691bec](https://github.com/tomtastisch/FileClassifier/commit/9691bec) | docs |
| `4.1.3` | Dokumentation: deterministische Restrukturierung mit SHA-fixierten Links und Policy-RoC-Gate | [90a2825](https://github.com/tomtastisch/FileClassifier/commit/90a2825) | docs |
| `4.1.2` | Versionierungs-Chore: Versionsbump für xUnit-v3-Migrationsrelease | [d1ed2a9](https://github.com/tomtastisch/FileClassifier/commit/d1ed2a9) | chore |
| `4.1.1` | Versionierungs-Chore: Patch-Version erhöht, um CI-Guard zu erfüllen | [d67050c](https://github.com/tomtastisch/FileClassifier/commit/d67050c) | chore |
| `4.1.0` | Version auf 4.1.0 aktualisiert und Versionierungslogik verbessert | [a3dfe23](https://github.com/tomtastisch/FileClassifier/commit/a3dfe23) | chore |
| `4.0.0` | Versionierungs-Basislinie auf 4.0.0 angehoben | [2a78f97](https://github.com/tomtastisch/FileClassifier/commit/2a78f97) | chore |
| `3.0.24` | Dokumentation: Umlaut-Schreibweisen korrigiert | [22d40b9](https://github.com/tomtastisch/FileClassifier/commit/22d40b9) | docs |
| `3.0.23` | Dokumentation: Markdown-Sprache normalisiert | [90310a0](https://github.com/tomtastisch/FileClassifier/commit/90310a0) | docs |
| `3.0.22` | Dokumentation: Struktur vereinheitlicht und Wartungs-Checklisten ergänzt | [cb3341d](https://github.com/tomtastisch/FileClassifier/commit/cb3341d) | docs |
| `3.0.21` | Dokumentation (Guides): Struktur vereinheitlicht und Schritt-für-Schritt-Checklisten mit Beispielen ergänzt | [5f5c6ab](https://github.com/tomtastisch/FileClassifier/commit/5f5c6ab) | docs |
| `3.0.20` | Dokumentation: Leitfäden für Optionen und Datentyp-Erweiterungen hinzugefügt (#5) | [392c628](https://github.com/tomtastisch/FileClassifier/commit/392c628) | docs |
| `3.0.19` | Dokumentation (Guides): Playbooks für Optionen und Datentyp-Erweiterungen hinzugefügt | [1427e1e](https://github.com/tomtastisch/FileClassifier/commit/1427e1e) | docs |
| `3.0.18` | Refactor: deterministisches Hashing, Archive-Härtung und Test/CI-Stabilisierung (#4) | [374732a](https://github.com/tomtastisch/FileClassifier/commit/374732a) | refactor |
| `3.0.17` | Tooling (Test): lesbares Ausgabeformat vereinfacht und technisches Testrauschen reduziert | [514922c](https://github.com/tomtastisch/FileClassifier/commit/514922c) | tooling |
| `3.0.16` | CI (Docs): Markdown-Heading-Anker im Link-Checker validiert | [02884ff](https://github.com/tomtastisch/FileClassifier/commit/02884ff) | ci |
| `3.0.15` | CI (Docs): Markdown-Link-Check als Gate hinzugefügt | [cdbdfbd](https://github.com/tomtastisch/FileClassifier/commit/cdbdfbd) | ci |
| `3.0.14` | Dokumentation (Struktur): README-Abdeckung erzwungen und Abstraction-Referenzen aktualisiert | [0d4cc5e](https://github.com/tomtastisch/FileClassifier/commit/0d4cc5e) | docs |
| `3.0.13` | Dokumentation (README): Grafik für Abstractions-Ordnerhierarchie hinzugefügt | [5853dd9](https://github.com/tomtastisch/FileClassifier/commit/5853dd9) | docs |
| `3.0.12` | Dokumentation (Abstractions): Referenzen nach Model-Ordnersplit finalisiert | [afdc592](https://github.com/tomtastisch/FileClassifier/commit/afdc592) | docs |
| `3.0.11` | refactor(abstractions): split models into detection archive hashing folders | [f25256f](https://github.com/tomtastisch/FileClassifier/commit/f25256f) | refactor |
| `3.0.10` | tooling(test): render bdd-readable output as clean per-test blocks | [268afe5](https://github.com/tomtastisch/FileClassifier/commit/268afe5) | tooling |
| `3.0.9` | test(core): add fail-closed internals coverage and raise ci gates | [27659c8](https://github.com/tomtastisch/FileClassifier/commit/27659c8) | test |
| `3.0.8` | refactor(tests): unify archive naming across archive-type tests | [093f372](https://github.com/tomtastisch/FileClassifier/commit/093f372) | refactor |
| `3.0.7` | docs(hash): add stage-by-stage deterministic test matrix | [c6dd754](https://github.com/tomtastisch/FileClassifier/commit/c6dd754) | docs |
| `3.0.6` | chore(version): introduce central deterministic project versioning | [b62ac79](https://github.com/tomtastisch/FileClassifier/commit/b62ac79) | chore |
| `3.0.5` | test(archive): table-drive archive hash and facade coverage | [1bc9b04](https://github.com/tomtastisch/FileClassifier/commit/1bc9b04) | test |
| `3.0.4` | refactor(test-docs): unify archive naming and add glossary | [c412613](https://github.com/tomtastisch/FileClassifier/commit/c412613) | refactor |
| `3.0.3` | test(e2e): erweitere hash-invarianz für archive positiv und negativ | [12dbe32](https://github.com/tomtastisch/FileClassifier/commit/12dbe32) | test |
| `3.0.2` | test(hash): prove archive extract-materialize hash invariance | [48c3f8b](https://github.com/tomtastisch/FileClassifier/commit/48c3f8b) | test |
| `3.0.1` | docs: vereinheitliche zip-terminologie auf archiv-kontext | [33eac47](https://github.com/tomtastisch/FileClassifier/commit/33eac47) | docs |
| `3.0.0` | refactor(config): centralize project options and hashing defaults | [fd03389](https://github.com/tomtastisch/FileClassifier/commit/fd03389) | breaking |
| `2.0.3` | refactor: vereinheitliche archive reason-codes und detector flow | [254ea22](https://github.com/tomtastisch/FileClassifier/commit/254ea22) | refactor |
| `2.0.2` | refactor: konsolidiere archive-entry flow und härte hash optionen | [d888f35](https://github.com/tomtastisch/FileClassifier/commit/d888f35) | refactor |
| `2.0.1` | docs: korrigiere reason-code referenzen für archive naming | [cffbb4b](https://github.com/tomtastisch/FileClassifier/commit/cffbb4b) | docs |
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
