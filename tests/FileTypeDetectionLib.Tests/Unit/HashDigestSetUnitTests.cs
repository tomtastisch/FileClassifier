using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class HashDigestSetUnitTests
{
    [Fact]
    public void Constructor_NormalizesAndLowercases()
    {
        var set = new HashDigestSet(
            " ABC ",
            "DeF",
            " 123 ",
            null,
            hasPhysicalHash: true,
            hasLogicalHash: false);

        Assert.Equal("abc", set.PhysicalSha256);
        Assert.Equal("def", set.LogicalSha256);
        Assert.Equal("123", set.FastPhysicalXxHash3);
        Assert.Equal(string.Empty, set.FastLogicalXxHash3);
        Assert.True(set.HasPhysicalHash);
        Assert.False(set.HasLogicalHash);
    }

    [Fact]
    public void Empty_ReturnsAllEmptyAndFalse()
    {
        var empty = HashDigestSet.Empty;

        Assert.Equal(string.Empty, empty.PhysicalSha256);
        Assert.Equal(string.Empty, empty.LogicalSha256);
        Assert.Equal(string.Empty, empty.FastPhysicalXxHash3);
        Assert.Equal(string.Empty, empty.FastLogicalXxHash3);
        Assert.False(empty.HasPhysicalHash);
        Assert.False(empty.HasLogicalHash);
    }
}