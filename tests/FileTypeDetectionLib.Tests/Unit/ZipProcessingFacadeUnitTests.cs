using System;
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

    [Fact]
    public void ExtractToDirectory_IsDisabled_AndDoesNotWriteToDisk()
    {
        var source = TestResources.Resolve("sample.zip");
        var tempRoot = Path.Combine(Path.GetTempPath(), "ftd-zipprocessing-facade-" + Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(tempRoot, "out");

        try
        {
            Directory.CreateDirectory(tempRoot);
#pragma warning disable CS0618
            var ok = ZipProcessing.ExtractToDirectory(source, destination, verifyBeforeExtract: true);
#pragma warning restore CS0618

            Assert.False(ok);
            Assert.False(Directory.Exists(destination));
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
