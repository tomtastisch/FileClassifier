using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class HeaderOnlyPolicyUnitTests
{
    [Fact]
    public void DefaultOptions_HeaderOnlyNonZip_IsTrue()
    {
        var options = FileTypeDetector.GetDefaultOptions();
        Assert.True(options.HeaderOnlyNonZip);
    }

    [Fact]
    public void Detect_StillRefines_ZipContainers_WhenHeaderOnlyNonZipIsTrue()
    {
        using var scope = new DetectorOptionsScope();
        scope.Set(new FileTypeDetectorOptions());

        var source = TestResources.Resolve("sample.docx");
        var detected = new FileTypeDetector().Detect(source);

        Assert.Equal(FileKind.Docx, detected.Kind);
    }

    [Fact]
    public void Detect_ReturnsZip_ForPlainZipWithoutOoxmlMarkers_WhenHeaderOnlyNonZipIsTrue()
    {
        using var scope = new DetectorOptionsScope();
        scope.Set(new FileTypeDetectorOptions());

        var source = TestResources.Resolve("sample.zip");
        var detected = new FileTypeDetector().Detect(source);

        Assert.Equal(FileKind.Zip, detected.Kind);
    }
}
