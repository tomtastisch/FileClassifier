using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DetectionDetailAndZipValidationUnitTests
{
    [Fact]
    public void DetectDetailed_ReturnsStructuredZipTrace_ForDocx()
    {
        var path = TestResources.Resolve("sample.docx");
        var detail = new FileTypeDetector().DetectDetailed(path);

        Assert.Equal(FileKind.Docx, detail.DetectedType.Kind);
        Assert.Equal("ZipStructuredRefined", detail.ReasonCode);
        Assert.True(detail.UsedZipContentCheck);
        Assert.True(detail.UsedStructuredRefinement);
        Assert.False(detail.ExtensionVerified);
    }

    [Fact]
    public void DetectDetailed_WithVerifyExtension_FailsClosed_OnExtensionMismatch()
    {
        var path = TestResources.Resolve("sample.changed.pdf.txt");
        var detail = new FileTypeDetector().DetectDetailed(path, verifyExtension: true);

        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
        Assert.Equal("ExtensionMismatch", detail.ReasonCode);
        Assert.False(detail.ExtensionVerified);
    }

    [Fact]
    public void TryValidateZip_ReturnsExpectedResult_ForKnownInputs()
    {
        var detector = new FileTypeDetector();

        Assert.True(detector.TryValidateZip(TestResources.Resolve("sample.zip")));
        Assert.True(detector.TryValidateZip(TestResources.Resolve("sample.docx")));
        Assert.False(detector.TryValidateZip(TestResources.Resolve("sample.pdf")));
    }

    [Fact]
    public void TryValidateZip_ReturnsFalse_ForMissingFile()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "ftd-missing-" + Guid.NewGuid().ToString("N") + ".zip");
        Assert.False(new FileTypeDetector().TryValidateZip(missingPath));
    }
}
