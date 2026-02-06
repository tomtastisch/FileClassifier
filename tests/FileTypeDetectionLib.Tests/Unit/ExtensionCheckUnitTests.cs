using System;
using System.IO;
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

    [Fact]
    public void Detect_DoesNotTrustFileNameExtension_WhenHeaderIsUnknown()
    {
        var detector = new FileTypeDetector();
        var path = Path.Combine(Path.GetTempPath(), "ftd-fake-" + Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, new byte[] { 0x01, 0x23, 0x45, 0x67 });

        try
        {
            var detected = detector.Detect(path);
            Assert.Equal(FileKind.Unknown, detected.Kind);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DetectAndVerifyExtension_AcceptsArchiveAlias_ForArchiveContent()
    {
        var detector = new FileTypeDetector();
        var source = TestResources.Resolve("sample.zip");
        var path = Path.Combine(Path.GetTempPath(), "ftd-zip-alias-" + Guid.NewGuid().ToString("N") + ".tar");

        File.Copy(source, path);
        try
        {
            var detected = detector.Detect(path);
            var verified = detector.DetectAndVerifyExtension(path);

            Assert.Equal(FileKind.Zip, detected.Kind);
            Assert.True(verified);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}