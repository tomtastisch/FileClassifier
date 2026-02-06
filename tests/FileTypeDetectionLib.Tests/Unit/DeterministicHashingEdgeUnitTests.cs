using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DeterministicHashingEdgeUnitTests
{
    [Fact]
    public void HashBytes_UsesDefaultLabel_WhenLabelEmpty()
    {
        var payload = new byte[] { 0x01, 0x02 };

        var evidence = DeterministicHashing.HashBytes(payload, "   ");

        Assert.Equal("payload.bin", evidence.Label);
        Assert.True(evidence.Digests.HasLogicalHash);
        Assert.True(evidence.Digests.HasPhysicalHash);
    }

    [Fact]
    public void HashEntries_AllowsEmptyEntries_ListReturnsNoEntry()
    {
        var evidence = DeterministicHashing.HashEntries(new List<ZipExtractedEntry>(), "entries");

        Assert.True(evidence.Digests.HasLogicalHash);
        Assert.Null(evidence.Entry);
        Assert.Equal(0, evidence.EntryCount);
        Assert.Equal(0, evidence.TotalUncompressedBytes);
    }
}