using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeDetectorFacadeUnitTests
{
    [Fact]
    public void Detect_ReturnsExpectedKind_ForKnownPdfFile()
    {
        var source = TestResources.Resolve("sample.pdf");
        var detected = new FileTypeDetector().Detect(source);

        Assert.Equal(FileKind.Pdf, detected.Kind);
        Assert.True(detected.Allowed);
    }

    [Fact]
    public void Detect_ReturnsUnknown_ForMissingPath()
    {
        var source = Path.Combine(Path.GetTempPath(), "ftd-missing-" + Guid.NewGuid().ToString("N") + ".bin");
        var detected = new FileTypeDetector().Detect(source);

        Assert.Equal(FileKind.Unknown, detected.Kind);
        Assert.False(detected.Allowed);
    }

    [Fact]
    public void DetectAndVerifyExtension_ReturnsFalse_ForMismatchedExtension()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-detector-facade");
        var wrongExtensionPath = Path.Combine(tempRoot.RootPath, "sample.txt");

        File.Copy(TestResources.Resolve("sample.pdf"), wrongExtensionPath);
        var ok = new FileTypeDetector().DetectAndVerifyExtension(wrongExtensionPath);

        Assert.False(ok);
    }
}