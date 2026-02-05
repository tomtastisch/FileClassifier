using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ZipProcessingFacadeUnitTests
{
    [Fact]
    public void TryValidate_ReturnsTrue_ForZipBytes_AndFalse_ForPdfBytes()
    {
        var zipBytes = File.ReadAllBytes(TestResources.Resolve("sample.zip"));
        var pdfBytes = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        Assert.True(ZipProcessing.TryValidate(zipBytes));
        Assert.False(ZipProcessing.TryValidate(pdfBytes));
    }

    [Fact]
    public void TryExtractToMemory_ReturnsEntries_ForValidZipBytes()
    {
        var zipBytes = File.ReadAllBytes(TestResources.Resolve("sample.zip"));

        var entries = ZipProcessing.TryExtractToMemory(zipBytes);

        Assert.Single(entries);
        Assert.Equal("note.txt", entries[0].RelativePath);
        Assert.NotEmpty(entries[0].Content);
    }
}
