# 04 - EvidenceHashing API Contract

## 1. Purpose and Scope
This document freezes the public contract of `EvidenceHashing`.
It is used as a pre-merge release baseline and as a guard against accidental API drift.

## 2. Contract Principles
- Deterministic: same input + same options => same digests.
- Fail-closed: invalid inputs return evidence objects with `Unknown`/empty digests instead of throwing as control flow.
- Security SSOT:
  - `PhysicalSha256` = raw bytes
  - `LogicalSha256` = canonicalized content
- `Fast*XxHash3` is optional and non-cryptographic.
- `Hmac*Sha256` is an optional keyed digest (HMAC-SHA256) for authenticity/tamper-evidence.
  - Enable: `HashOptions.IncludeSecureHash = true`
  - Key: environment variable `FILECLASSIFIER_HMAC_KEY_B64` (Base64)
  - If the key is missing/invalid: HMAC digests stay empty and evidence notes include a clear message.
- Overloads without `options` use `FileTypeOptions.GetSnapshot().DeterministicHash` as the global default policy.

## 3. Public Surface (approved)
| Method | Return | Contract |
|---|---|---|
| `HashFile(path)` | `HashEvidence` | hash from file, with archive canonicalization when applicable |
| `HashFile(path, options)` | `HashEvidence` | same as above, options-driven |
| `HashBytes(data)` | `HashEvidence` | hash from payload |
| `HashBytes(data, label)` | `HashEvidence` | same as above, with label for evidence |
| `HashBytes(data, label, options)` | `HashEvidence` | same as above, options-driven |
| `HashEntries(entries)` | `HashEvidence` | canonical hash from an entry list |
| `HashEntries(entries, label)` | `HashEvidence` | same as above, with label for evidence |
| `HashEntries(entries, label, options)` | `HashEvidence` | same as above, options-driven |
| `VerifyRoundTrip(path)` | `HashRoundTripReport` | h1-h4 evidence for file/archive |
| `VerifyRoundTrip(path, options)` | `HashRoundTripReport` | same as above, options-driven |

## 4. h1-h4 Semantics
- `h1`: initial input (`HashFile`)
- `h2`: archive-logical entries or direct bytes
- `h3`: hash over canonical bytes
- `h4`: hash after materialization and re-read
- Strict equality contract:
  - `h1.logical == h2.logical == h3.logical == h4.logical`
  - `h1.physical` may differ from `h2/h3/h4` for archives.

## 5. Verification (Gate)
```bash
dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj --filter "FullyQualifiedName~HashingEvidenceApiContractTests" -v minimal
```

## 6. Approval Note
The contract is considered approved when:
1. public-surface tests are green,
2. h1-h4 semantic tests are green,
3. CI coverage gates (line+branch) are green.

## Documentation Maintenance Checklist
- [ ] Content verified against current code state.
- [ ] Links and anchors checked with `python3 tools/check-docs.py`.
- [ ] Examples/commands verified locally.
- [ ] Terminology aligned with `docs/110_API_CORE.MD`.
