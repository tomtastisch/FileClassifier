<!-- LANG_SWITCH:BEGIN -->
[DE](001_HASHING_RENAME.MD) | [EN](101_HASHING_RENAME.MD)
<!-- LANG_SWITCH:END -->

# Migration: Hashing Rename (Deterministic* -> Evidence/Hash*)

## Scope
This document maps the breaking public API rename of the hashing/evidence subsystem.
Behavior remains deterministic and fail-closed; identifiers and file names changed.

## Type Renames
| Old | New |
|---|---|
| `DeterministicHashing` | `EvidenceHashing` |
| `DeterministicHashEvidence` | `HashEvidence` |
| `DeterministicHashDigestSet` | `HashDigestSet` |
| `DeterministicHashOptions` | `HashOptions` |
| `DeterministicHashRoundTripReport` | `HashRoundTripReport` |
| `DeterministicHashSourceType` | `HashSourceType` |

## File Renames (Repository)
| Old Path | New Path |
|---|---|
| `src/FileTypeDetection/DeterministicHashing.vb` | `src/FileTypeDetection/EvidenceHashing.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashEvidence.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashEvidence.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashDigestSet.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashDigestSet.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashOptions.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashOptions.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashRoundTripReport.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashRoundTripReport.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashSourceType.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashSourceType.vb` |

Note:
- The `Old Path` entries are historical and do not exist on `main` after the rename commit.
- Repo-local verification:
  - `git log --follow --oneline -- src/FileTypeDetection/DeterministicHashing.vb`
  - `git show <baseline_sha>:src/FileTypeDetection/DeterministicHashing.vb`

## Optional Secure Digests (HMAC-SHA256)
New optional digests were added:
- `HashDigestSet.HmacPhysicalSha256`
- `HashDigestSet.HmacLogicalSha256`

Opt-in:
- Set `HashOptions.IncludeSecureHash = true`.
- Provide a Base64 key via `FILECLASSIFIER_HMAC_KEY_B64`.

If secure hashing is requested but the key is missing or invalid:
- Existing SHA-256 and xxHash3 behavior is preserved.
- HMAC digests are empty.
- `HashEvidence.Notes` includes a clear message that HMAC was requested but omitted.

## JSON Options (FileTypeOptions)
The JSON keys for the global hashing options remain under the existing `deterministicHash` namespace.
New key:
- `deterministicHash.includeSecureHash` (object form)
- `deterministicHashIncludeSecureHash` (flat form)
