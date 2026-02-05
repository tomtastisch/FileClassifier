using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DetectionDetailUnitTests
{
    [Fact]
    public void Constructor_NormalizesNulls()
    {
        var detail = new DetectionDetail(null, null, usedZipContentCheck: false, usedStructuredRefinement: true, extensionVerified: false);

        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
        Assert.Equal(string.Empty, detail.ReasonCode);
        Assert.False(detail.UsedZipContentCheck);
        Assert.True(detail.UsedStructuredRefinement);
        Assert.False(detail.ExtensionVerified);
    }
}
