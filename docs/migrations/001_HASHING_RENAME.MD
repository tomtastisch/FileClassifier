<!-- LANG_SWITCH:BEGIN -->
[DE](001_HASHING_RENAME.MD) | [EN](101_HASHING_RENAME.MD)
<!-- LANG_SWITCH:END -->

# Migration: Hashing-Rename (Deterministic* -> Evidence/Hash*)

## Zweck und Scope
Dieses Dokument mappt das Breaking-Rename der öffentlichen API im Hashing/Evidence-Subsystem.
Das Verhalten bleibt deterministisch und fail-closed; nur Identifier und Dateinamen wurden umbenannt.

## Type-Renames
| Alt | Neu |
|---|---|
| `DeterministicHashing` | `EvidenceHashing` |
| `DeterministicHashEvidence` | `HashEvidence` |
| `DeterministicHashDigestSet` | `HashDigestSet` |
| `DeterministicHashOptions` | `HashOptions` |
| `DeterministicHashRoundTripReport` | `HashRoundTripReport` |
| `DeterministicHashSourceType` | `HashSourceType` |

## File-Renames (Repository)
| Alter Pfad | Neuer Pfad |
|---|---|
| `src/FileTypeDetection/DeterministicHashing.vb` | `src/FileTypeDetection/EvidenceHashing.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashEvidence.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashEvidence.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashDigestSet.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashDigestSet.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashOptions.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashOptions.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashRoundTripReport.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashRoundTripReport.vb` |
| `src/FileTypeDetection/Abstractions/Hashing/DeterministicHashSourceType.vb` | `src/FileTypeDetection/Abstractions/Hashing/HashSourceType.vb` |

Hinweis:
- Die Einträge unter `Alter Pfad` sind historisch und existieren auf `main` nach dem Rename-Commit nicht mehr.
- Repo-lokale Verifikation:
  - `git log --follow --oneline -- src/FileTypeDetection/DeterministicHashing.vb`
  - `git show <baseline_sha>:src/FileTypeDetection/DeterministicHashing.vb`

## Optionale Secure Digests (HMAC-SHA256)
Neue optionale Digests wurden hinzugefügt:
- `HashDigestSet.HmacPhysicalSha256`
- `HashDigestSet.HmacLogicalSha256`

Aktivierung:
- Set `HashOptions.IncludeSecureHash = true`.
- Provide a Base64 key via `FILECLASSIFIER_HMAC_KEY_B64`.

Wenn Secure Hashing angefordert wird, aber der Key fehlt oder ungültig ist:
- Das bestehende SHA-256 und xxHash3 Verhalten bleibt erhalten.
- HMAC-Digests sind leer.
- `HashEvidence.Notes` enthält eine klare Nachricht, dass HMAC angefordert wurde, aber nicht gesetzt war.

## JSON-Optionen (FileTypeOptions)
Die JSON-Keys für die globalen Hashing-Optionen bleiben unter dem bestehenden Namespace `deterministicHash`.
Neuer Key:
- `deterministicHash.includeSecureHash` (object form)
- `deterministicHashIncludeSecureHash` (flat form)
