using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ExtensionCheckUnitTests
{
    [Fact]
    public void DetectAndVerifyExtension_ReturnsTrue_ForPdfPath()
    {
        var path = TestResources.Resolve("sample.pdf");
        var detector = new FileTypeDetector();

        var detected = detector.Detect(path);
        var verified = detector.DetectAndVerifyExtension(path);

        Assert.True(verified, $"detected={detected.Kind}, path={path}");
    }
}
