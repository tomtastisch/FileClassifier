using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class EvidenceHashingUnitTests
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
        Assert.Equal(first.Digests.FastPhysicalXxHash3, second.Digests.FastPhysicalXxHash3);
        Assert.Equal(first.Digests.FastLogicalXxHash3, second.Digests.FastLogicalXxHash3);
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
}