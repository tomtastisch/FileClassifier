<!-- LANG_SWITCH:BEGIN -->
[DE](000_HASHING_BASELINE.MD) | [EN](100_HASHING_BASELINE.MD)
<!-- LANG_SWITCH:END -->

# Hashing-Baseline Evidence (Historischer Snapshot: vor EvidenceHashing-Rename)

Erfasst am: 2026-02-10

Pinned Baseline-Commit (vor Rename):
- `373146f0d2c178533ea6ac442e6cc2a117cb4331`

Rename-Commit (Deterministic* -> Evidence/Hash*):
- `7c6e94dad37b440c32386719e796d0bb6100c741`

## Zweck und Scope
Dieses Dokument ist historische Evidence fuer das Verhalten der Hashing-Fassade und des Evidence-Modells **vor** dem public Rename:
- `DeterministicHashing` -> `EvidenceHashing`
- `DeterministicHash*` -> `Hash*`

Es darf nicht als Beschreibung der aktuellen API-Oberflaeche auf `main` verwendet werden.
SSOT fuer den aktuellen Stand:
- API-Surface: `docs/010_API_CORE.MD`
- Contract: `docs/contracts/001_CONTRACT_HASHING.MD`
- Implementation: `src/FileTypeDetection/EvidenceHashing.vb`
- Models: `src/FileTypeDetection/Abstractions/Hashing/*`

## Verifikation (repo-lokal, deterministisch)
Alle Referenzen unten sind auf den Baseline-Commit gepinnt (via `git show`):

```bash
BASE=373146f0d2c178533ea6ac442e6cc2a117cb4331

git show "$BASE":src/FileTypeDetection/DeterministicHashing.vb | nl -ba | sed -n '1,260p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashEvidence.vb | nl -ba | sed -n '1,220p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashDigestSet.vb | nl -ba | sed -n '1,240p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashOptions.vb | nl -ba | sed -n '1,240p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashRoundTripReport.vb | nl -ba | sed -n '1,260p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashSourceType.vb | nl -ba | sed -n '1,120p'
```

## Baseline-Fassade (vor Rename)
Pinned File (Baseline-Commit):
- `src/FileTypeDetection/DeterministicHashing.vb` @ `373146f0`

Ausgewaehlte public Entrypoints (Baseline):
- `HashFile(path)` / `HashFile(path, options)`
- `HashBytes(data)` / `HashBytes(data, label, options)`
- `HashEntries(entries, label, options)`
- `VerifyRoundTrip(path, options)`

## Semantik der kanonischen Bytes (Baseline)
Pinned File: `src/FileTypeDetection/DeterministicHashing.vb` @ `373146f0`

Raw-Payload-Mode (`BuildEvidenceFromRawPayload`):
- Physical SHA-256 und Logical SHA-256 werden beide aus denselben `safePayload` Bytes berechnet.
- Fast-Hashes werden beide aus denselben `safePayload` Bytes berechnet.

Archive/Entries-Mode (`BuildEvidenceFromEntries`):
- Logical Bytes sind ein kanonisches Manifest (`BuildLogicalManifestBytes`) aus normalisierten Entries.
- Physical Bytes sind die Original-Archive-Bytes nur dann, wenn `compressedBytes` vorhanden und nicht leer ist.

Kanonische Manifest-Struktur:
- Enthaelt `LogicalManifestVersion` Header und Entry-Count.
- Pro Entry: schreibt Path-Bytes + Content-Length + SHA-256(content) Bytes.

Formatierung:
- SHA-256 Hex: lowercase (`ToLowerInvariant()` nach `Convert.ToHexString(...)`).
- xxHash3: lowercase fixed-width `x16` via `ToString("x16", CultureInfo.InvariantCulture)`.
- Wenn `IncludeFastHash` false ist (oder options null), sind Fast-Digests leer.

## Evidence-Model Types (Baseline-Namen)
Pinned Files @ `373146f0`:
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashDigestSet.vb`
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashEvidence.vb`
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashOptions.vb`
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashRoundTripReport.vb`
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashSourceType.vb`

## Current Implementation Pointers (nach Rename)
Aktuelle Fassade + Models auf `main`:
- `src/FileTypeDetection/EvidenceHashing.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashDigestSet.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashEvidence.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashOptions.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashRoundTripReport.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashSourceType.vb`

## Baseline-Tests
Historisches Kommando (Baseline):
- `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal`

Hinweis:
- Die aktuelle Test-Suite-Groesse und exakte Counts koennen von der Baseline abweichen; der autoritative Check ist der CI-Job `tests-bdd-coverage` und lokal `dotnet test` auf `main`.
