using System.Globalization;
using System.IO.Hashing;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

internal static class HashingEvidenceTestHelpers
{
    internal const string LogicalManifestVersion = "FTD-LOGICAL-HASH-V1";

    internal const string HmacKeyEnvVarB64 = "FILECLASSIFIER_HMAC_KEY_B64";

    // Bytes 0..31 Base64-encoded.
    internal const string TestHmacKeyB64 = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    internal static readonly object HmacEnvLock = new();

    internal static byte[] BuildLogicalManifestBytes(IReadOnlyList<ZipExtractedEntry> entries)
    {
        var normalizedEntries = entries
            .Select(entry => new
            {
                RelativePath = entry.RelativePath,
                Content = entry.Content.IsDefaultOrEmpty ? Array.Empty<byte>() : entry.Content.ToArray()
            })
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal)
            .ToArray();

        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            var versionBytes = Encoding.UTF8.GetBytes(LogicalManifestVersion);
            writer.Write(versionBytes.Length);
            writer.Write(versionBytes);
            writer.Write(normalizedEntries.Length);

            foreach (var entry in normalizedEntries)
            {
                var pathBytes = Encoding.UTF8.GetBytes(entry.RelativePath);
                var contentHash = SHA256.HashData(entry.Content);

                writer.Write(pathBytes.Length);
                writer.Write(pathBytes);
                writer.Write((long)entry.Content.Length);
                writer.Write(contentHash.Length);
                writer.Write(contentHash);
            }
        }

        return ms.ToArray();
    }

    internal static string ComputeHmacSha256Hex(byte[] key, byte[] payload)
    {
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(payload)).ToLowerInvariant();
    }
}

// Section 1: SHA256 physical vs logical behavior
public sealed class HashingEvidenceSha256Tests
{
    [Fact]
    public void HashBytes_ReturnsStableDigests_ForSamePayload()
    {
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        var first = EvidenceHashing.HashBytes(payload, "sample.pdf");
        var second = EvidenceHashing.HashBytes(payload, "sample.pdf");

        Assert.True(first.Digests.HasPhysicalHash);
        Assert.True(first.Digests.HasLogicalHash);
        Assert.Equal(first.Digests.PhysicalSha256, second.Digests.PhysicalSha256);
        Assert.Equal(first.Digests.LogicalSha256, second.Digests.LogicalSha256);
    }

    [Fact]
    public void HashEntries_IsOrderIndependent_ForLogicalDigest()
    {
        var a = new ZipExtractedEntry("b.txt", new byte[] { 0x02, 0x03 });
        var b = new ZipExtractedEntry("a.txt", new byte[] { 0x01 });

        var first = EvidenceHashing.HashEntries(new[] { a, b }, "entries");
        var second = EvidenceHashing.HashEntries(new[] { b, a }, "entries");

        Assert.True(first.Digests.HasLogicalHash);
        Assert.True(second.Digests.HasLogicalHash);
        Assert.Equal(first.Digests.LogicalSha256, second.Digests.LogicalSha256);
        Assert.Equal(2, first.EntryCount);
        Assert.Equal(3, first.TotalUncompressedBytes);
    }

    [Fact]
    public void HashEntries_FailsClosed_ForPathTraversal()
    {
        var invalid = new ZipExtractedEntry("../evil.txt", new byte[] { 0x42 });

        var evidence = EvidenceHashing.HashEntries(new[] { invalid }, "invalid");

        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.Equal(FileKind.Unknown, evidence.DetectedType.Kind);
    }

    [Fact]
    public void HashBytes_ForArchive_AlignsWithHashEntriesLogicalDigest()
    {
        var zipBytes = ArchivePayloadFactory.CreateZipWithSingleEntry("inner/note.txt", "hello");

        var fromArchiveBytes = EvidenceHashing.HashBytes(zipBytes, "sample.zip");
        var entries = ArchiveProcessing.TryExtractToMemory(zipBytes);
        var fromEntries = EvidenceHashing.HashEntries(entries, "entries");

        Assert.True(fromArchiveBytes.Digests.HasLogicalHash);
        Assert.True(fromArchiveBytes.Digests.HasPhysicalHash);
        Assert.True(fromEntries.Digests.HasLogicalHash);
        Assert.Equal(fromArchiveBytes.Digests.LogicalSha256, fromEntries.Digests.LogicalSha256);
    }

    [Fact]
    public void HashFile_And_HashBytes_Match_ForPlainFile()
    {
        var path = TestResources.Resolve("sample.pdf");
        var payload = File.ReadAllBytes(path);

        var fromFile = EvidenceHashing.HashFile(path);
        var fromBytes = EvidenceHashing.HashBytes(payload, "sample.pdf");

        Assert.Equal(fromFile.Digests.PhysicalSha256, fromFile.Digests.LogicalSha256);
        Assert.Equal(fromBytes.Digests.PhysicalSha256, fromBytes.Digests.LogicalSha256);
        Assert.Equal(fromFile.Digests.PhysicalSha256, fromBytes.Digests.PhysicalSha256);
        Assert.Equal(fromFile.Digests.LogicalSha256, fromBytes.Digests.LogicalSha256);
    }

    [Fact]
    public void HashBytes_RespectsIncludePayloadCopiesOption()
    {
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));
        var withoutCopies = EvidenceHashing.HashBytes(payload, "sample.pdf",
            new HashOptions { IncludePayloadCopies = false });
        var withCopies = EvidenceHashing.HashBytes(payload, "sample.pdf",
            new HashOptions { IncludePayloadCopies = true });

        Assert.True(withoutCopies.CompressedBytes.IsDefaultOrEmpty);
        Assert.True(withoutCopies.UncompressedBytes.IsDefaultOrEmpty);
        Assert.False(withCopies.CompressedBytes.IsDefaultOrEmpty);
        Assert.Equal(payload.Length, withCopies.CompressedBytes.Length);
        Assert.Equal(payload, withCopies.UncompressedBytes.ToArray());
    }

    [Fact]
    public void HashBytes_UsesGlobalHashOptions_WhenNoOptionsProvided()
    {
        using var scope = new DetectorOptionsScope();
        var global = FileTypeDetector.GetDefaultOptions();
        global.DeterministicHash = new HashOptions
        {
            IncludeFastHash = false,
            IncludePayloadCopies = false,
            MaterializedFileName = "../global-evidence.bin"
        };
        scope.Set(global);

        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));
        var evidence = EvidenceHashing.HashBytes(payload, "sample.pdf");
        var overrideEvidence = EvidenceHashing.HashBytes(payload, "sample.pdf",
            new HashOptions { IncludeFastHash = true });

        Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.FastPhysicalXxHash3));
        Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.FastLogicalXxHash3));
        Assert.False(string.IsNullOrWhiteSpace(overrideEvidence.Digests.FastPhysicalXxHash3));
    }

    [Fact]
    public void HashBytes_FallsBackToArchiveByteMode_WhenArchivePayloadIsUnsafe()
    {
        var payload = ArchiveEntryPayloadFactory.CreateZipWithSingleEntry("../evil.txt", 8);
        var evidence = EvidenceHashing.HashBytes(payload, "traversal.zip");

        Assert.True(evidence.Digests.HasPhysicalHash);
        Assert.True(evidence.Digests.HasLogicalHash);
        Assert.Equal(evidence.Digests.PhysicalSha256, evidence.Digests.LogicalSha256);
        Assert.Equal(1, evidence.EntryCount);
    }

    [Fact]
    public void HashBytes_ReturnsFailure_ForNullPayload()
    {
        var evidence = EvidenceHashing.HashBytes(null, "payload.bin");

        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.False(evidence.Digests.HasPhysicalHash);
        Assert.Contains("Payload", evidence.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashBytes_ReturnsFailure_WhenPayloadExceedsMaxBytes()
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxBytes = 4;
        scope.Set(options);

        var payload = new byte[8];
        var evidence = EvidenceHashing.HashBytes(payload, "large.bin");

        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.Contains("MaxBytes", evidence.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashEntries_ReturnsFailure_ForNullEntries()
    {
        var evidence = EvidenceHashing.HashEntries(null, "entries");

        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.Contains("Entries", evidence.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashEntries_ReturnsFailure_ForNullEntryElement()
    {
        var entries = new List<ZipExtractedEntry?> { null };
        var evidence = EvidenceHashing.HashEntries(entries, "entries");

        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.Contains("Entry", evidence.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashEntries_ReturnsFailure_ForDuplicateNormalizedPath()
    {
        var a = new ZipExtractedEntry("a.txt", new byte[] { 0x01 });
        var b = new ZipExtractedEntry("a.txt", new byte[] { 0x02 });

        var evidence = EvidenceHashing.HashEntries(new[] { a, b }, "entries");

        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.Contains("Doppelter", evidence.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashFile_ReturnsFailure_ForMissingFile()
    {
        var evidence = EvidenceHashing.HashFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin"));

        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.Contains("nicht", evidence.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashEntries_ReturnsFailure_ForDuplicateNormalizedPath_AfterTrim()
    {
        var a = new ZipExtractedEntry("a.txt", new byte[] { 0x01 });
        var b = new ZipExtractedEntry("a.txt ", new byte[] { 0x02 });

        var evidence = EvidenceHashing.HashEntries(new[] { a, b }, "entries");

        Assert.False(evidence.Digests.HasLogicalHash);
    }

    [Fact]
    public void HashBytes_UsesDefaultLabel_WhenLabelEmpty()
    {
        var payload = new byte[] { 0x01, 0x02 };

        var evidence = EvidenceHashing.HashBytes(payload, "   ");

        Assert.Equal("payload.bin", evidence.Label);
        Assert.True(evidence.Digests.HasLogicalHash);
        Assert.True(evidence.Digests.HasPhysicalHash);
    }

    [Fact]
    public void HashEntries_AllowsEmptyEntries_ListReturnsNoEntry()
    {
        var evidence = EvidenceHashing.HashEntries(new List<ZipExtractedEntry>(), "entries");

        Assert.True(evidence.Digests.HasLogicalHash);
        Assert.Null(evidence.Entry);
        Assert.Equal(0, evidence.EntryCount);
        Assert.Equal(0, evidence.TotalUncompressedBytes);
    }

    [Fact]
    public void EvidenceHashing_HashBytes_UsesLoadedIncludePayloadCopies()
    {
        var original = FileTypeOptions.GetSnapshot();
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"deterministicHashIncludePayloadCopies\":false}"));

            var evidence = EvidenceHashing.HashBytes(payload, "sample.pdf");

            Assert.True(evidence.CompressedBytes.IsDefaultOrEmpty);
            Assert.True(evidence.UncompressedBytes.IsDefaultOrEmpty);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }
}

// Section 2: xxHash3 behavior (IncludeFastHash on/off, formatting, stability)
public sealed class HashingEvidenceXxHash3Tests
{
    [Fact]
    public void HashBytes_RespectsIncludeFastHashOption()
    {
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));
        var withoutFast = EvidenceHashing.HashBytes(payload, "sample.pdf",
            new HashOptions { IncludeFastHash = false });
        var withFast = EvidenceHashing.HashBytes(payload, "sample.pdf",
            new HashOptions { IncludeFastHash = true });

        Assert.True(string.IsNullOrWhiteSpace(withoutFast.Digests.FastPhysicalXxHash3));
        Assert.True(string.IsNullOrWhiteSpace(withoutFast.Digests.FastLogicalXxHash3));
        Assert.False(string.IsNullOrWhiteSpace(withFast.Digests.FastPhysicalXxHash3));
        Assert.False(string.IsNullOrWhiteSpace(withFast.Digests.FastLogicalXxHash3));
    }

    [Fact]
    public void FastHash_ForRawPayload_MatchesXxHash3_UInt64LowerHex()
    {
        var payload = new byte[] { 0x00, 0x01, 0x02, 0xFF };
        var evidence = EvidenceHashing.HashBytes(payload, "payload.bin", new HashOptions { IncludeFastHash = true });

        var expected = XxHash3.HashToUInt64(payload).ToString("x16", CultureInfo.InvariantCulture);
        Assert.Equal(expected, evidence.Digests.FastPhysicalXxHash3);
        Assert.Equal(expected, evidence.Digests.FastLogicalXxHash3);

        Assert.Matches(new Regex("^[0-9a-f]{16}$"), evidence.Digests.FastPhysicalXxHash3);
        Assert.Matches(new Regex("^[0-9a-f]{16}$"), evidence.Digests.FastLogicalXxHash3);
    }

    [Fact]
    public void FastHash_ForArchiveBytes_HashesPhysicalBytesAndCanonicalLogicalManifest()
    {
        var zipBytes = ArchivePayloadFactory.CreateZipWithSingleEntry("inner/note.txt", "hello");
        var evidence = EvidenceHashing.HashBytes(zipBytes, "sample.zip", new HashOptions { IncludeFastHash = true });

        Assert.True(evidence.Digests.HasPhysicalHash);
        Assert.True(evidence.Digests.HasLogicalHash);

        var expectedPhysicalFast = XxHash3.HashToUInt64(zipBytes).ToString("x16", CultureInfo.InvariantCulture);
        Assert.Equal(expectedPhysicalFast, evidence.Digests.FastPhysicalXxHash3);

        var extractedEntries = ArchiveProcessing.TryExtractToMemory(zipBytes);
        var logicalManifestBytes = HashingEvidenceTestHelpers.BuildLogicalManifestBytes(extractedEntries);

        var expectedLogicalSha256 = Convert.ToHexString(SHA256.HashData(logicalManifestBytes)).ToLowerInvariant();
        var expectedLogicalFast = XxHash3.HashToUInt64(logicalManifestBytes).ToString("x16", CultureInfo.InvariantCulture);

        Assert.Equal(expectedLogicalSha256, evidence.Digests.LogicalSha256);
        Assert.Equal(expectedLogicalFast, evidence.Digests.FastLogicalXxHash3);
    }

    [Fact]
    public void EvidenceHashing_HashBytes_UsesLoadedIncludeFastHash()
    {
        var original = FileTypeOptions.GetSnapshot();
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"deterministicHashIncludeFastHash\":false}"));

            var evidence = EvidenceHashing.HashBytes(payload, "sample.pdf");

            Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.FastPhysicalXxHash3));
            Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.FastLogicalXxHash3));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void ComputeFastHash_ReturnsEmpty_WhenOptionDisabled()
    {
        var method = typeof(EvidenceHashing).GetMethod("ComputeFastHash", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var options = new HashOptions { IncludeFastHash = false };
        var result = TestGuard.NotNull(method.Invoke(null, new object?[] { new byte[] { 1, 2, 3 }, options }) as string);

        Assert.Equal(string.Empty, result);
    }
}

// Section 3: HMAC behavior (IncludeSecureHash on/off; key set/missing/invalid; stability)
public sealed class HashingEvidenceHmacTests
{
    [Fact]
    public void EvidenceHashing_HashBytes_UsesLoadedIncludeSecureHash()
    {
        var original = FileTypeOptions.GetSnapshot();
        var payload = new byte[] { 0x10, 0x20, 0x30 };
        var key = Convert.FromBase64String(HashingEvidenceTestHelpers.TestHmacKeyB64);

        lock (HashingEvidenceTestHelpers.HmacEnvLock)
        {
            var prior = Environment.GetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64);
            try
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64,
                    HashingEvidenceTestHelpers.TestHmacKeyB64);
                Assert.True(FileTypeOptions.LoadOptions(
                    "{\"deterministicHashIncludeSecureHash\":true,\"deterministicHashIncludeFastHash\":false}"));

                var evidence = EvidenceHashing.HashBytes(payload, "payload.bin");
                var expected = HashingEvidenceTestHelpers.ComputeHmacSha256Hex(key, payload);

                Assert.Equal(expected, evidence.Digests.HmacPhysicalSha256);
                Assert.Equal(expected, evidence.Digests.HmacLogicalSha256);
            }
            finally
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64, prior);
                FileTypeOptions.SetSnapshot(original);
            }
        }
    }

    [Fact]
    public void SecureHash_WhenDisabled_LeavesHmacDigestsEmpty()
    {
        var payload = new byte[] { 0x10, 0x20, 0x30 };
        var evidence = EvidenceHashing.HashBytes(payload, "payload.bin",
            new HashOptions { IncludeSecureHash = false, IncludeFastHash = false });

        Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.HmacPhysicalSha256));
        Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.HmacLogicalSha256));
    }

    [Fact]
    public void SecureHash_WhenEnabledAndKeyValid_ComputesHmacForRawPayload()
    {
        var payload = new byte[] { 0x10, 0x20, 0x30 };
        var key = Convert.FromBase64String(HashingEvidenceTestHelpers.TestHmacKeyB64);

        lock (HashingEvidenceTestHelpers.HmacEnvLock)
        {
            var prior = Environment.GetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64);
            try
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64,
                    HashingEvidenceTestHelpers.TestHmacKeyB64);

                var evidence = EvidenceHashing.HashBytes(payload, "payload.bin",
                    new HashOptions { IncludeSecureHash = true, IncludeFastHash = false });

                var expected = HashingEvidenceTestHelpers.ComputeHmacSha256Hex(key, payload);
                Assert.Equal(expected, evidence.Digests.HmacPhysicalSha256);
                Assert.Equal(expected, evidence.Digests.HmacLogicalSha256);
                Assert.DoesNotContain("HMAC digests omitted", evidence.Notes, StringComparison.Ordinal);
            }
            finally
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64, prior);
            }
        }
    }

    [Fact]
    public void SecureHash_WhenEnabledAndKeyMissing_LeavesHmacEmpty_AndAppendsNote()
    {
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        lock (HashingEvidenceTestHelpers.HmacEnvLock)
        {
            var prior = Environment.GetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64);
            try
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64, null);

                var evidence = EvidenceHashing.HashBytes(payload, "payload.bin",
                    new HashOptions { IncludeSecureHash = true, IncludeFastHash = false });

                Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.HmacPhysicalSha256));
                Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.HmacLogicalSha256));
                Assert.Contains(HashingEvidenceTestHelpers.HmacKeyEnvVarB64, evidence.Notes, StringComparison.Ordinal);
                Assert.Contains("HMAC digests omitted", evidence.Notes, StringComparison.Ordinal);
            }
            finally
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64, prior);
            }
        }
    }

    [Fact]
    public void SecureHash_WhenEnabledAndKeyInvalidBase64_LeavesHmacEmpty_AndAppendsNote()
    {
        var payload = new byte[] { 0x10, 0x20, 0x30 };

        lock (HashingEvidenceTestHelpers.HmacEnvLock)
        {
            var prior = Environment.GetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64);
            try
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64, "!!!not-base64!!!");

                var evidence = EvidenceHashing.HashBytes(payload, "payload.bin",
                    new HashOptions { IncludeSecureHash = true, IncludeFastHash = false });

                Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.HmacPhysicalSha256));
                Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.HmacLogicalSha256));
                Assert.Contains("invalid Base64", evidence.Notes, StringComparison.Ordinal);
            }
            finally
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64, prior);
            }
        }
    }

    [Fact]
    public void SecureHash_WhenEnabledForArchiveBytes_ComputesPhysicalAndLogicalHmacFromCorrectBytes()
    {
        var zipBytes = ArchivePayloadFactory.CreateZipWithSingleEntry("inner/note.txt", "hello");
        var key = Convert.FromBase64String(HashingEvidenceTestHelpers.TestHmacKeyB64);

        lock (HashingEvidenceTestHelpers.HmacEnvLock)
        {
            var prior = Environment.GetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64);
            try
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64,
                    HashingEvidenceTestHelpers.TestHmacKeyB64);

                var evidence = EvidenceHashing.HashBytes(zipBytes, "sample.zip",
                    new HashOptions { IncludeSecureHash = true, IncludeFastHash = false });

                Assert.True(evidence.Digests.HasPhysicalHash);
                Assert.True(evidence.Digests.HasLogicalHash);

                var extractedEntries = ArchiveProcessing.TryExtractToMemory(zipBytes);
                var logicalManifestBytes = HashingEvidenceTestHelpers.BuildLogicalManifestBytes(extractedEntries);

                var expectedPhysical = HashingEvidenceTestHelpers.ComputeHmacSha256Hex(key, zipBytes);
                var expectedLogical = HashingEvidenceTestHelpers.ComputeHmacSha256Hex(key, logicalManifestBytes);

                Assert.Equal(expectedPhysical, evidence.Digests.HmacPhysicalSha256);
                Assert.Equal(expectedLogical, evidence.Digests.HmacLogicalSha256);
            }
            finally
            {
                Environment.SetEnvironmentVariable(HashingEvidenceTestHelpers.HmacKeyEnvVarB64, prior);
            }
        }
    }
}

// Section 4: RoundTrip (h1-h4) consistency for both raw and archive cases (hash-specific assertions only)
public sealed class HashingEvidenceRoundTripTests
{
    public static TheoryData<string> ArchiveFixtureCases()
    {
        return new TheoryData<string>
        {
            "fx.sample_zip",
            "fx.sample_rar",
            "fx.sample_7z"
        };
    }

    public static TheoryData<string, bool> RoundTripCases()
    {
        return new TheoryData<string, bool>
        {
            { "fx.sample_zip", true },
            { "fx.sample_7z", true },
            { "fx.sample_rar", true },
            { "fx.sample_pdf", false }
        };
    }

    public static TheoryData<string> DeterminismCases()
    {
        return new TheoryData<string>
        {
            "fx.sample_zip",
            "fx.sample_7z",
            "fx.sample_rar",
            "fx.sample_pdf"
        };
    }

    [Fact]
    public void LogicalHash_IsStableAcrossArchiveTarAndTarGz_ForSameContent()
    {
        var zip = ArchivePayloadFactory.CreateZipWithSingleEntry("inner/note.txt", "hello");
        var tar = ArchivePayloadFactory.CreateTarWithSingleEntry("inner/note.txt", "hello");
        var tarGz = ArchivePayloadFactory.CreateTarGzWithSingleEntry("inner/note.txt", "hello");

        var zipEvidence = EvidenceHashing.HashBytes(zip, "sample.zip");
        var tarEvidence = EvidenceHashing.HashBytes(tar, "sample.tar");
        var tarGzEvidence = EvidenceHashing.HashBytes(tarGz, "sample.tar.gz");

        Assert.Equal(zipEvidence.Digests.LogicalSha256, tarEvidence.Digests.LogicalSha256);
        Assert.Equal(zipEvidence.Digests.LogicalSha256, tarGzEvidence.Digests.LogicalSha256);
        Assert.NotEqual(zipEvidence.Digests.PhysicalSha256, tarEvidence.Digests.PhysicalSha256);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void VerifyRoundTrip_ProducesLogicalConsistency(string fixtureId, bool expectedArchive)
    {
        var path = TestResources.Resolve(fixtureId);
        var report = EvidenceHashing.VerifyRoundTrip(path);

        Assert.Equal(expectedArchive, report.IsArchiveInput);
        Assert.True(report.LogicalConsistent);
        Assert.True(report.LogicalH1EqualsH2);
        Assert.True(report.LogicalH1EqualsH3);
        Assert.True(report.LogicalH1EqualsH4);
    }

    [Theory]
    [MemberData(nameof(DeterminismCases))]
    public void HashFile_Determinism_HoldsAcrossRepeatedFixtureRuns(string fixtureId)
    {
        var path = TestResources.Resolve(fixtureId);
        var first = EvidenceHashing.HashFile(path);
        var second = EvidenceHashing.HashFile(path);

        Assert.Equal(first.Digests.LogicalSha256, second.Digests.LogicalSha256);
        Assert.Equal(first.Digests.PhysicalSha256, second.Digests.PhysicalSha256);
        Assert.Equal(first.Digests.FastLogicalXxHash3, second.Digests.FastLogicalXxHash3);
    }

    [Theory]
    [MemberData(nameof(ArchiveFixtureCases))]
    public void ArchivePipeline_PreservesCombinedAndPerFileHashes_AfterExtractByteMaterializeAndRecheck(
        string fixtureId)
    {
        var path = TestResources.Resolve(fixtureId);
        Assert.True(ArchiveProcessing.TryValidate(path));

        var extractedEntries = ArchiveProcessing.ExtractToMemory(path, true);
        Assert.NotEmpty(extractedEntries);

        var archiveEvidence = EvidenceHashing.HashFile(path);
        var extractedCombinedEvidence = EvidenceHashing.HashEntries(extractedEntries, $"{fixtureId}-extracted");
        Assert.True(archiveEvidence.Digests.HasLogicalHash);
        Assert.True(extractedCombinedEvidence.Digests.HasLogicalHash);
        Assert.Equal(archiveEvidence.Digests.LogicalSha256, extractedCombinedEvidence.Digests.LogicalSha256);

        using var scope = TestTempPaths.CreateScope($"ftd-materialized-{fixtureId}");
        var originalPerFileLogical = new Dictionary<string, string>(StringComparer.Ordinal);
        var rematerializedEntries = new List<ZipExtractedEntry>();

        foreach (var entry in extractedEntries.OrderBy(e => e.RelativePath, StringComparer.Ordinal))
        {
            var entryBytes = entry.Content.ToArray();
            var fromBytes = EvidenceHashing.HashBytes(entryBytes, entry.RelativePath);
            Assert.True(fromBytes.Digests.HasLogicalHash);
            Assert.True(fromBytes.Digests.HasPhysicalHash);
            Assert.Equal(fromBytes.Digests.PhysicalSha256, fromBytes.Digests.LogicalSha256);
            originalPerFileLogical[entry.RelativePath] = fromBytes.Digests.LogicalSha256;

            var destinationPath = Path.Combine(scope.RootPath, ToPlatformRelativePath(entry.RelativePath));
            Assert.True(FileMaterializer.Persist(entryBytes, destinationPath, false, false));

            var fromMaterializedFile = EvidenceHashing.HashFile(destinationPath);
            Assert.True(fromMaterializedFile.Digests.HasLogicalHash);
            Assert.True(fromMaterializedFile.Digests.HasPhysicalHash);
            Assert.Equal(fromBytes.Digests.LogicalSha256, fromMaterializedFile.Digests.LogicalSha256);
            Assert.Equal(fromBytes.Digests.PhysicalSha256, fromMaterializedFile.Digests.PhysicalSha256);

            rematerializedEntries.Add(new ZipExtractedEntry(entry.RelativePath, File.ReadAllBytes(destinationPath)));
        }

        var rematerializedCombinedEvidence =
            EvidenceHashing.HashEntries(rematerializedEntries, $"{fixtureId}-materialized");
        Assert.True(rematerializedCombinedEvidence.Digests.HasLogicalHash);
        Assert.Equal(extractedCombinedEvidence.Digests.LogicalSha256,
            rematerializedCombinedEvidence.Digests.LogicalSha256);
        Assert.Equal(archiveEvidence.Digests.LogicalSha256, rematerializedCombinedEvidence.Digests.LogicalSha256);

        foreach (var entry in rematerializedEntries.OrderBy(e => e.RelativePath, StringComparer.Ordinal))
        {
            var hashedEntry = EvidenceHashing.HashBytes(entry.Content.ToArray(), entry.RelativePath);
            Assert.True(originalPerFileLogical.TryGetValue(entry.RelativePath, out var expectedLogical));
            Assert.Equal(expectedLogical, hashedEntry.Digests.LogicalSha256);
        }
    }

    [Theory]
    [MemberData(nameof(ArchiveFixtureCases))]
    public void ArchiveBytePipeline_PreservesCombinedAndPerFileHashes_AfterExtractAndMaterialize(string fixtureId)
    {
        var path = TestResources.Resolve(fixtureId);
        var archiveBytes = File.ReadAllBytes(path);
        Assert.True(ArchiveProcessing.TryValidate(archiveBytes));

        var extractedEntries = ArchiveProcessing.TryExtractToMemory(archiveBytes);
        Assert.NotEmpty(extractedEntries);

        var archiveFromBytesEvidence = EvidenceHashing.HashBytes(archiveBytes, $"{fixtureId}-bytes");
        var extractedCombinedEvidence = EvidenceHashing.HashEntries(extractedEntries, $"{fixtureId}-entries");
        Assert.True(archiveFromBytesEvidence.Digests.HasLogicalHash);
        Assert.True(extractedCombinedEvidence.Digests.HasLogicalHash);
        Assert.Equal(archiveFromBytesEvidence.Digests.LogicalSha256, extractedCombinedEvidence.Digests.LogicalSha256);

        using var scope = TestTempPaths.CreateScope($"ftd-bytes-materialized-{fixtureId}");
        var rematerializedEntries = new List<ZipExtractedEntry>();

        foreach (var entry in extractedEntries.OrderBy(e => e.RelativePath, StringComparer.Ordinal))
        {
            var entryBytes = entry.Content.ToArray();
            var perEntryEvidence = EvidenceHashing.HashBytes(entryBytes, entry.RelativePath);
            var destinationPath = Path.Combine(scope.RootPath, ToPlatformRelativePath(entry.RelativePath));
            Assert.True(FileMaterializer.Persist(entryBytes, destinationPath, false, false));

            var perMaterializedFileEvidence = EvidenceHashing.HashFile(destinationPath);
            Assert.Equal(perEntryEvidence.Digests.LogicalSha256, perMaterializedFileEvidence.Digests.LogicalSha256);
            Assert.Equal(perEntryEvidence.Digests.PhysicalSha256, perMaterializedFileEvidence.Digests.PhysicalSha256);

            rematerializedEntries.Add(new ZipExtractedEntry(entry.RelativePath, File.ReadAllBytes(destinationPath)));
        }

        var rematerializedCombinedEvidence =
            EvidenceHashing.HashEntries(rematerializedEntries, $"{fixtureId}-bytes-materialized");
        Assert.Equal(extractedCombinedEvidence.Digests.LogicalSha256,
            rematerializedCombinedEvidence.Digests.LogicalSha256);
        Assert.Equal(archiveFromBytesEvidence.Digests.LogicalSha256,
            rematerializedCombinedEvidence.Digests.LogicalSha256);
    }

    [Fact]
    public void UnsafeArchiveCandidate_FailsExtraction_AndFallsBackToArchiveByteHashing()
    {
        var payload = ArchiveEntryPayloadFactory.CreateZipWithSingleEntry("../evil.txt", 8);

        Assert.Empty(ArchiveProcessing.TryExtractToMemory(payload));

        var evidence = EvidenceHashing.HashBytes(payload, "unsafe-archive-candidate.zip");
        Assert.True(evidence.Digests.HasLogicalHash);
        Assert.True(evidence.Digests.HasPhysicalHash);
        Assert.Equal(evidence.Digests.PhysicalSha256, evidence.Digests.LogicalSha256);
    }

    [Fact]
    public void VerifyRoundTrip_FailsClosed_ForMissingFile()
    {
        var report = EvidenceHashing.VerifyRoundTrip(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin"));

        Assert.False(report.LogicalConsistent);
        Assert.Contains("missing", report.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyRoundTrip_ReturnsFailure_WhenH1MissingLogicalDigest()
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxBytes = 1;
        scope.Set(options);

        var path = TestResources.Resolve("sample.pdf");
        var report = EvidenceHashing.VerifyRoundTrip(path);

        Assert.False(report.H1.Digests.HasLogicalHash);
        Assert.Contains("h1", report.Notes, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToPlatformRelativePath(string relativePath)
    {
        return relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
    }
}

// Model/Reflection guards for hashing evidence types
public sealed class HashingEvidenceModelTests
{
    [Fact]
    public void HashDigestSet_Constructor_NormalizesAndLowercases()
    {
        var set = new HashDigestSet(
            " ABC ",
            "DeF",
            " 123 ",
            null,
            " 0A ",
            null,
            hasPhysicalHash: true,
            hasLogicalHash: false);

        Assert.Equal("abc", set.PhysicalSha256);
        Assert.Equal("def", set.LogicalSha256);
        Assert.Equal("123", set.FastPhysicalXxHash3);
        Assert.Equal(string.Empty, set.FastLogicalXxHash3);
        Assert.Equal("0a", set.HmacPhysicalSha256);
        Assert.Equal(string.Empty, set.HmacLogicalSha256);
        Assert.True(set.HasPhysicalHash);
        Assert.False(set.HasLogicalHash);
    }

    [Fact]
    public void HashDigestSet_Empty_ReturnsAllEmptyAndFalse()
    {
        var empty = HashDigestSet.Empty;

        Assert.Equal(string.Empty, empty.PhysicalSha256);
        Assert.Equal(string.Empty, empty.LogicalSha256);
        Assert.Equal(string.Empty, empty.FastPhysicalXxHash3);
        Assert.Equal(string.Empty, empty.FastLogicalXxHash3);
        Assert.Equal(string.Empty, empty.HmacPhysicalSha256);
        Assert.Equal(string.Empty, empty.HmacLogicalSha256);
        Assert.False(empty.HasPhysicalHash);
        Assert.False(empty.HasLogicalHash);
    }

    [Fact]
    public void HashEvidence_Constructor_SetsSafeDefaults_WhenInputsNull()
    {
        var evidence = new HashEvidence(
            sourceType: HashSourceType.Unknown,
            label: null,
            detectedType: null,
            entry: null,
            compressedBytes: null,
            uncompressedBytes: null,
            entryCount: -1,
            totalUncompressedBytes: -5,
            digests: null,
            notes: null);

        Assert.Equal(string.Empty, evidence.Label);
        Assert.Equal(FileKind.Unknown, evidence.DetectedType.Kind);
        Assert.Null(evidence.Entry);
        Assert.Equal(0, evidence.EntryCount);
        Assert.Equal(0, evidence.TotalUncompressedBytes);
        Assert.True(evidence.CompressedBytes.IsDefaultOrEmpty);
        Assert.True(evidence.UncompressedBytes.IsDefaultOrEmpty);
        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.False(evidence.Digests.HasPhysicalHash);
        Assert.Equal(string.Empty, evidence.Notes);
    }

    [Fact]
    public void HashOptions_Normalize_ReturnsDefaults_WhenOptionsNull()
    {
        var normalized = HashOptions.Normalize(null);

        Assert.Equal("deterministic-roundtrip.bin", normalized.MaterializedFileName);
        Assert.True(normalized.IncludeFastHash);
        Assert.False(normalized.IncludeSecureHash);
    }

    [Fact]
    public void HashOptions_Normalize_FallsBack_ForInvalidFilenameChars()
    {
        var normalized = HashOptions.Normalize(new HashOptions
        {
            MaterializedFileName = "inv\0alid.bin"
        });

        Assert.Equal("deterministic-roundtrip.bin", normalized.MaterializedFileName);
    }

    [Fact]
    public void HashOptions_Normalize_FallsBack_ForWhitespaceName()
    {
        var normalized = HashOptions.Normalize(new HashOptions
        {
            MaterializedFileName = "   "
        });

        Assert.Equal("deterministic-roundtrip.bin", normalized.MaterializedFileName);
    }

    [Fact]
    public void HashOptions_Normalize_FallsBack_WhenFileNameIsRoot()
    {
        var normalized = HashOptions.Normalize(new HashOptions
        {
            MaterializedFileName = "/"
        });

        Assert.Equal("deterministic-roundtrip.bin", normalized.MaterializedFileName);
    }

    [Fact]
    public void HashOptions_Normalize_StripsPathSegments()
    {
        var normalized = HashOptions.Normalize(new HashOptions
        {
            MaterializedFileName = "nested/inner/evidence.bin"
        });

        Assert.Equal("evidence.bin", normalized.MaterializedFileName);
    }

    [Fact]
    public void HashOptions_Normalize_SanitizesMaterializedFileNameToFileName()
    {
        var normalized = HashOptions.Normalize(new HashOptions
        {
            MaterializedFileName = "../nested/../../evidence.bin"
        });

        Assert.Equal("evidence.bin", normalized.MaterializedFileName);
    }

    [Fact]
    public void HashOptions_Normalize_FallsBackForEmptyMaterializedFileName()
    {
        var normalized = HashOptions.Normalize(new HashOptions
        {
            MaterializedFileName = "   "
        });

        Assert.Equal("deterministic-roundtrip.bin", normalized.MaterializedFileName);
    }

    [Fact]
    public void ResolveHashOptions_FallsBack_WhenProjectOptionsNull()
    {
        var method = typeof(EvidenceHashing).GetMethod("ResolveHashOptions", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var result = TestGuard.NotNull(method.Invoke(null, new object?[] { null, null }) as HashOptions);

        Assert.NotNull(result);
        Assert.Equal("deterministic-roundtrip.bin", result.MaterializedFileName);
    }

    [Fact]
    public void NormalizedEntry_Defaults_WhenConstructedWithNulls()
    {
        var type = typeof(EvidenceHashing).GetNestedTypes(BindingFlags.NonPublic)
            .First(t => t.Name == "NormalizedEntry");

        var ctor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .First();

        var instance = ctor.Invoke(new object?[] { null, null });
        var relativePath =
            (string)type.GetField("RelativePath", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
        var content =
            (byte[])type.GetField("Content", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;

        Assert.Equal(string.Empty, relativePath);
        Assert.NotNull(content);
        Assert.Empty(content);
    }

    [Fact]
    public void HashRoundTripReport_Constructor_DefaultsToFailureEvidence_WhenInputsNull()
    {
        var report = new HashRoundTripReport("", isArchiveInput: false, h1: null, h2: null, h3: null,
            h4: null, notes: null);

        Assert.False(report.LogicalConsistent);
        Assert.False(report.LogicalH1EqualsH2);
        Assert.False(report.LogicalH1EqualsH3);
        Assert.False(report.LogicalH1EqualsH4);
        Assert.False(report.PhysicalH1EqualsH2);
        Assert.False(report.PhysicalH1EqualsH3);
        Assert.False(report.PhysicalH1EqualsH4);
    }

    [Fact]
    public void HashRoundTripReport_Constructor_ReportsConsistency_WhenLogicalAndPhysicalMatch()
    {
        var digest = new HashDigestSet(
            physicalSha256: "a",
            logicalSha256: "b",
            fastPhysicalXxHash3: string.Empty,
            fastLogicalXxHash3: string.Empty,
            hmacPhysicalSha256: string.Empty,
            hmacLogicalSha256: string.Empty,
            hasPhysicalHash: true,
            hasLogicalHash: true);

        var evidence = new HashEvidence(
            sourceType: HashSourceType.RawBytes,
            label: "x",
            detectedType: FileTypeRegistry.Resolve(FileKind.Unknown),
            entry: null,
            compressedBytes: new byte[] { 0x01 },
            uncompressedBytes: new byte[] { 0x01 },
            entryCount: 1,
            totalUncompressedBytes: 1,
            digests: digest,
            notes: "ok");

        var report = new HashRoundTripReport("x", isArchiveInput: false, h1: evidence, h2: evidence,
            h3: evidence, h4: evidence, notes: "ok");

        Assert.True(report.LogicalConsistent);
        Assert.True(report.LogicalH1EqualsH2);
        Assert.True(report.LogicalH1EqualsH3);
        Assert.True(report.LogicalH1EqualsH4);
        Assert.True(report.PhysicalH1EqualsH2);
        Assert.True(report.PhysicalH1EqualsH3);
        Assert.True(report.PhysicalH1EqualsH4);
    }

    [Fact]
    public void HashRoundTripReport_Constructor_DistinguishesPhysicalWhenLogicalMissing()
    {
        var digest = new HashDigestSet(
            physicalSha256: "abc",
            logicalSha256: string.Empty,
            fastPhysicalXxHash3: string.Empty,
            fastLogicalXxHash3: string.Empty,
            hmacPhysicalSha256: string.Empty,
            hmacLogicalSha256: string.Empty,
            hasPhysicalHash: true,
            hasLogicalHash: false);

        var evidence = new HashEvidence(
            sourceType: HashSourceType.RawBytes,
            label: "x",
            detectedType: FileTypeRegistry.Resolve(FileKind.Unknown),
            entry: null,
            compressedBytes: new byte[] { 0x01 },
            uncompressedBytes: new byte[] { 0x01 },
            entryCount: 1,
            totalUncompressedBytes: 1,
            digests: digest,
            notes: "ok");

        var report = new HashRoundTripReport("x", isArchiveInput: false, h1: evidence, h2: evidence,
            h3: evidence, h4: evidence, notes: "ok");

        Assert.False(report.LogicalH1EqualsH2);
        Assert.True(report.PhysicalH1EqualsH2);
    }

    [Fact]
    public void HashRoundTripReport_EqualLogical_ReturnsFalse_WhenEvidenceNull()
    {
        var method = typeof(HashRoundTripReport)
            .GetMethod("EqualLogical", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = TestGuard.Unbox<bool>(method.Invoke(null, new object?[] { null, null }));

        Assert.False(result);
    }

    [Fact]
    public void HashRoundTripReport_EqualPhysical_ReturnsFalse_WhenEvidenceNull()
    {
        var method = typeof(HashRoundTripReport)
            .GetMethod("EqualPhysical", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = TestGuard.Unbox<bool>(method.Invoke(null, new object?[] { null, null }));

        Assert.False(result);
    }

    [Fact]
    public void NormalizeLabel_FallsBack_ForNullOrWhitespace()
    {
        var method = typeof(EvidenceHashing).GetMethod("NormalizeLabel", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var label1 = TestGuard.NotNull(method.Invoke(null, new object?[] { null }) as string);
        var label2 = TestGuard.NotNull(method.Invoke(null, new object?[] { "   " }) as string);

        Assert.Equal("payload.bin", label1);
        Assert.Equal("payload.bin", label2);
    }

    [Fact]
    public void CopyBytes_ReturnsEmpty_ForNullOrEmpty()
    {
        var method = typeof(EvidenceHashing).GetMethod("CopyBytes", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var empty1 = TestGuard.NotNull(method.Invoke(null, new object?[] { null }) as byte[]);
        var empty2 = TestGuard.NotNull(method.Invoke(null, new object?[] { Array.Empty<byte>() }) as byte[]);

        Assert.Empty(empty1);
        Assert.Empty(empty2);
    }

    [Fact]
    public void TryReadFileBounded_ReturnsFalse_ForMissingPathOrOptions()
    {
        var method = typeof(EvidenceHashing).GetMethod("TryReadFileBounded", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var bytes = Array.Empty<byte>();
        var error = string.Empty;

        object[] args1 = { string.Empty, FileTypeProjectOptions.DefaultOptions(), bytes, error };
        var ok1 = TestGuard.Unbox<bool>(method.Invoke(null, args1));
        Assert.False(ok1);

        object[] args2 = { "missing", null!, bytes, error };
        var ok2 = TestGuard.Unbox<bool>(method.Invoke(null, args2));
        Assert.False(ok2);
    }

    [Fact]
    public void TryReadFileBounded_ReturnsFalse_WhenFileTooLarge()
    {
        var method = typeof(EvidenceHashing).GetMethod("TryReadFileBounded", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-hash-read");
        var path = Path.Combine(scope.RootPath, "big.bin");
        File.WriteAllBytes(path, new byte[10]);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxBytes = 4;

        object[] args = { path, opt, Array.Empty<byte>(), string.Empty };
        var ok = TestGuard.Unbox<bool>(method.Invoke(null, args));

        Assert.False(ok);
    }
}

[Trait("Category", "ApiContract")]
public sealed class HashingEvidenceApiContractTests
{
    private static readonly string[] sourceArray =
    {
        "HashBytes(Byte[]):HashEvidence",
        "HashBytes(Byte[],String):HashEvidence",
        "HashBytes(Byte[],String,HashOptions):HashEvidence",
        "HashEntries(IReadOnlyList`1):HashEvidence",
        "HashEntries(IReadOnlyList`1,String):HashEvidence",
        "HashEntries(IReadOnlyList`1,String,HashOptions):HashEvidence",
        "HashFile(String):HashEvidence",
        "HashFile(String,HashOptions):HashEvidence",
        "VerifyRoundTrip(String):HashRoundTripReport",
        "VerifyRoundTrip(String,HashOptions):HashRoundTripReport"
    };

    [Fact]
    public void PublicStaticSurface_MatchesApprovedContract()
    {
        var methods = typeof(EvidenceHashing)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == typeof(EvidenceHashing))
            .Select(Describe)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var expected = sourceArray.OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, methods);
    }

    [Fact]
    public void HashFile_MissingPath_FailsClosedWithoutThrowing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.bin");
        var evidence = EvidenceHashing.HashFile(missingPath);

        Assert.Equal(FileKind.Unknown, evidence.DetectedType.Kind);
        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.False(evidence.Digests.HasPhysicalHash);
    }

    private static string Describe(MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Select(p => p.ParameterType.Name)
            .ToArray();
        return $"{method.Name}({string.Join(",", parameters)}):{method.ReturnType.Name}";
    }
}
