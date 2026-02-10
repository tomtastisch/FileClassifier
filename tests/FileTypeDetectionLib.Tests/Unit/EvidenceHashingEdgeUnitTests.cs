using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class EvidenceHashingEdgeUnitTests
{
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
}