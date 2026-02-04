using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

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
        var tempRoot = Path.Combine(Path.GetTempPath(), "ftd-detector-facade-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var wrongExtensionPath = Path.Combine(tempRoot, "sample.txt");

        try
        {
            File.Copy(TestResources.Resolve("sample.pdf"), wrongExtensionPath);
            var ok = new FileTypeDetector().DetectAndVerifyExtension(wrongExtensionPath);

            Assert.False(ok);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
