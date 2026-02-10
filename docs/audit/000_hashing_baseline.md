# Hashing Baseline Evidence (Commit 0)

Date: 2026-02-10
Branch: feature/secure-hash-hmac-finalization (from origin/main)

## Scope
Baseline evidence for the current hashing facade + evidence models before any renames or behavior changes.

## Public facade and behavior
### Facade class name + API surface
File: src/FileTypeDetection/DeterministicHashing.vb
- `Public NotInheritable Class DeterministicHashing` (line 14)
- Public entrypoints (selected):
  - `HashFile(path)` / `HashFile(path, options)` (lines 21-62)
  - `HashBytes(data)` / `HashBytes(data, label, options)` (lines 64-109)
  - `HashEntries(entries, label, options)` (lines 111-132)
  - `VerifyRoundTrip(path, options)` (lines 134-224)

### Canonical bytes used for SHA256 and xxHash3
File: src/FileTypeDetection/DeterministicHashing.vb

Raw payload mode (`BuildEvidenceFromRawPayload`):
- Physical SHA256 and Logical SHA256 are both computed from the same `safePayload` bytes.
- Fast hashes are both computed from the same `safePayload` bytes.

Evidence snippet:
- `physicalSha = ComputeSha256Hex(safePayload)`
- `logicalSha = physicalSha`
- `fastPhysical = ComputeFastHash(safePayload, hashOptions)`
- `fastLogical = fastPhysical`
(lines 295-309)

Archive/entries mode (`BuildEvidenceFromEntries`):
- Logical bytes are a canonical manifest (`BuildLogicalManifestBytes`) derived from normalized entries.
- Physical bytes are the original archive bytes only when `compressedBytes` is present/non-empty.

Evidence snippet:
- `logicalBytes = BuildLogicalManifestBytes(normalizedEntries)`
- `logicalSha = ComputeSha256Hex(logicalBytes)`
- `fastLogical = ComputeFastHash(logicalBytes, hashOptions)`
- `If compressedBytes ...` then:
  - `physicalSha = ComputeSha256Hex(compressedBytes)`
  - `fastPhysical = ComputeFastHash(compressedBytes, hashOptions)`
(lines 241-252)

Canonical manifest structure:
- Includes `LogicalManifestVersion` header and entry count.
- For each entry: writes path bytes + content length + SHA256(content) bytes.

Evidence snippet:
- `Dim contentHash = SHA256.HashData(entry.Content)`
- `writer.Write(contentHash.Length)`
- `writer.Write(contentHash)`
(lines 370-386)

### SHA256 hex formatting
File: src/FileTypeDetection/DeterministicHashing.vb
- `Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant()`
(lines 392-395)

### xxHash3 implementation + formatting
File: src/FileTypeDetection/DeterministicHashing.vb
- Uses `System.IO.Hashing.XxHash3.HashToUInt64(data)`
- Formats as lowercase fixed-width 16 hex digits: `ToString("x16", CultureInfo.InvariantCulture)`
- When `IncludeFastHash` is false (or options is null), returns `String.Empty`.

Evidence snippet:
- `If options Is Nothing OrElse Not options.IncludeFastHash Then Return String.Empty`
- `Dim value = System.IO.Hashing.XxHash3.HashToUInt64(data)`
- `Return value.ToString("x16", CultureInfo.InvariantCulture)`
(lines 397-402)

## Evidence model types
### Digest set
File: src/FileTypeDetection/Abstractions/Hashing/DeterministicHashDigestSet.vb
- Public properties:
  - `PhysicalSha256`, `LogicalSha256`
  - `FastPhysicalXxHash3`, `FastLogicalXxHash3`
  - `HasPhysicalHash`, `HasLogicalHash`
- Normalization:
  - `Trim().ToLowerInvariant()` (Normalize)

### Evidence wrapper
File: src/FileTypeDetection/Abstractions/Hashing/DeterministicHashEvidence.vb
- Has `Notes As String` and stores bytes as `ImmutableArray(Of Byte)`.
- Failures created via `CreateFailure(...)` with empty bytes and `DeterministicHashDigestSet.Empty`.

### Options
File: src/FileTypeDetection/Abstractions/Hashing/DeterministicHashOptions.vb
- Defaults:
  - `IncludePayloadCopies = False`
  - `IncludeFastHash = True`
  - `MaterializedFileName = "deterministic-roundtrip.bin"`

### RoundTrip report
File: src/FileTypeDetection/Abstractions/Hashing/DeterministicHashRoundTripReport.vb
- Reports h1..h4 and computes logical/physical equality based on `LogicalSha256` / `PhysicalSha256`.

### Source type
File: src/FileTypeDetection/Abstractions/Hashing/DeterministicHashSourceType.vb
- Values: Unknown, FilePath, RawBytes, ArchiveEntries, MaterializedFile.

## Baseline tests
Command:
- `dotnet test tests/FileTypeDetectionLib.Tests/FileTypeDetectionLib.Tests.csproj -v minimal`

Result (captured from console output):
- Exit code: 0
- Passed: 394
- Failed: 0
