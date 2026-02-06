using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DetectionDetailAndArchiveValidationUnitTests
{
    [Fact]
    public void DetectDetailed_ReturnsStructuredArchiveTrace_ForDocx()
    {
        var path = TestResources.Resolve("sample.docx");
        var detail = new FileTypeDetector().DetectDetailed(path);

        Assert.Equal(FileKind.Docx, detail.DetectedType.Kind);
        Assert.Equal("ArchiveStructuredRefined", detail.ReasonCode);
        Assert.True(detail.UsedZipContentCheck);
        Assert.True(detail.UsedStructuredRefinement);
        Assert.False(detail.ExtensionVerified);
    }

    [Fact]
    public void DetectDetailed_WithVerifyExtension_FailsClosed_OnExtensionMismatch()
    {
        var path = TestResources.Resolve("sample.changed.pdf.txt");
        var detail = new FileTypeDetector().DetectDetailed(path, true);

        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
        Assert.Equal("ExtensionMismatch", detail.ReasonCode);
        Assert.False(detail.ExtensionVerified);
    }

    [Fact]
    public void TryValidateArchive_ReturnsExpectedResult_ForKnownInputs()
    {
        Assert.True(FileTypeDetector.TryValidateArchive(TestResources.Resolve("sample.zip")));
        Assert.True(FileTypeDetector.TryValidateArchive(TestResources.Resolve("sample.docx")));
        Assert.False(FileTypeDetector.TryValidateArchive(TestResources.Resolve("sample.pdf")));
    }

    [Fact]
    public void TryValidateArchive_ReturnsFalse_ForMissingFile()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "ftd-missing-" + Guid.NewGuid().ToString("N") + ".zip");
        Assert.False(FileTypeDetector.TryValidateArchive(missingPath));
    }
}
