using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeDetectorExtensionUnitTests
{
    [Fact]
    public void DetectAndVerifyExtension_ReturnsFalse_ForUnknownKind()
    {
        var detector = new FileTypeDetector();
        var result = detector.DetectAndVerifyExtension("sample.unknown");
        Assert.False(result);
    }

    [Fact]
    public void DetectAndVerifyExtension_ReturnsTrue_WhenNoExtension()
    {
        using var scope = TestTempPaths.CreateScope("ftd-noext");
        var path = Path.Combine(scope.RootPath, "file");
        File.Copy(TestResources.Resolve("sample.pdf"), path);

        var detector = new FileTypeDetector();
        var result = detector.DetectAndVerifyExtension(path);

        Assert.True(result);
    }
}