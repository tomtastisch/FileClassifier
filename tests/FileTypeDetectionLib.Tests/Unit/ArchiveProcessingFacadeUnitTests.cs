using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveProcessingFacadeUnitTests
{
    [Fact]
    public void TryValidate_ReturnsTrue_ForArchiveBytes_AndFalse_ForPdfBytes()
    {
        var zipBytes = File.ReadAllBytes(TestResources.Resolve("sample.zip"));
        var pdfBytes = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        Assert.True(ArchiveProcessing.TryValidate(zipBytes));
        Assert.False(ArchiveProcessing.TryValidate(pdfBytes));
    }

    [Fact]
    public void TryExtractToMemory_ReturnsEntries_ForValidArchiveBytes()
    {
        var zipBytes = File.ReadAllBytes(TestResources.Resolve("sample.zip"));

        var entries = ArchiveProcessing.TryExtractToMemory(zipBytes);

        Assert.Single(entries);
        Assert.Equal("note.txt", entries[0].RelativePath);
        Assert.NotEmpty(entries[0].Content);
    }
}
