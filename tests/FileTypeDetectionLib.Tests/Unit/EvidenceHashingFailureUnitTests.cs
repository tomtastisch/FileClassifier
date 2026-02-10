using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class EvidenceHashingFailureUnitTests
{
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
}
