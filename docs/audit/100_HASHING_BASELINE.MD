<!-- LANG_SWITCH:BEGIN -->
[DE](000_HASHING_BASELINE.MD) | [EN](100_HASHING_BASELINE.MD)
<!-- LANG_SWITCH:END -->

# Hashing Baseline Evidence (Historical Snapshot: pre EvidenceHashing rename)

Date captured: 2026-02-10

Pinned baseline commit (pre rename):
- `373146f0d2c178533ea6ac442e6cc2a117cb4331`

Rename commit (Deterministic* -> Evidence/Hash*):
- `7c6e94dad37b440c32386719e796d0bb6100c741`

## Scope
This document is historical evidence for the hashing facade + evidence model behavior **before** the public rename:
- `DeterministicHashing` -> `EvidenceHashing`
- `DeterministicHash*` -> `Hash*`

It must not be used as a description of the current API surface on `main`.
For current SSOT:
- API surface: `docs/110_API_CORE.MD`
- Contract: `docs/contracts/101_CONTRACT_HASHING.MD`
- Implementation: `src/FileTypeDetection/EvidenceHashing.vb`
- Models: `src/FileTypeDetection/Abstractions/Hashing/*`

## How To Verify (repo-local, deterministic)
All references below are pinned to the baseline commit via `git show`:

```bash
BASE=373146f0d2c178533ea6ac442e6cc2a117cb4331

git show "$BASE":src/FileTypeDetection/DeterministicHashing.vb | nl -ba | sed -n '1,260p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashEvidence.vb | nl -ba | sed -n '1,220p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashDigestSet.vb | nl -ba | sed -n '1,240p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashOptions.vb | nl -ba | sed -n '1,240p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashRoundTripReport.vb | nl -ba | sed -n '1,260p'
git show "$BASE":src/FileTypeDetection/Abstractions/Hashing/DeterministicHashSourceType.vb | nl -ba | sed -n '1,120p'
```

## Baseline Facade (pre rename)
Pinned file (baseline commit):
- `src/FileTypeDetection/DeterministicHashing.vb` @ `373146f0`

Selected public entrypoints (baseline):
- `HashFile(path)` / `HashFile(path, options)`
- `HashBytes(data)` / `HashBytes(data, label, options)`
- `HashEntries(entries, label, options)`
- `VerifyRoundTrip(path, options)`

## Canonical Bytes Semantics (baseline)
Pinned file: `src/FileTypeDetection/DeterministicHashing.vb` @ `373146f0`

Raw payload mode (`BuildEvidenceFromRawPayload`):
- Physical SHA-256 and Logical SHA-256 are both computed from the same `safePayload` bytes.
- Fast hashes are both computed from the same `safePayload` bytes.

Archive/entries mode (`BuildEvidenceFromEntries`):
- Logical bytes are a canonical manifest (`BuildLogicalManifestBytes`) derived from normalized entries.
- Physical bytes are the original archive bytes only when `compressedBytes` is present/non-empty.

Canonical manifest structure:
- Includes `LogicalManifestVersion` header and entry count.
- For each entry: writes path bytes + content length + SHA-256(content) bytes.

Formatting:
- SHA-256 hex: lowercase (`ToLowerInvariant()` after `Convert.ToHexString(...)`).
- xxHash3: lowercase fixed-width `x16` via `ToString("x16", CultureInfo.InvariantCulture)`.
- If `IncludeFastHash` is false (or options is null), fast digests are empty.

## Evidence Model Types (baseline names)
Pinned files @ `373146f0`:
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashDigestSet.vb`
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashEvidence.vb`
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashOptions.vb`
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashRoundTripReport.vb`
- `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashSourceType.vb`

## Current Implementation Pointers (post rename)
Current facade + models on `main`:
- `src/FileTypeDetection/EvidenceHashing.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashDigestSet.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashEvidence.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashOptions.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashRoundTripReport.vb`
- `src/FileTypeDetection/Abstractions/Hashing/HashSourceType.vb`

## Baseline Tests
Historical command (baseline):
- `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal`

Note:
- The current test suite size and exact counts can differ from the baseline snapshot; the authoritative check is the CI job `tests-bdd-coverage` and local `dotnet test` on `main`.
