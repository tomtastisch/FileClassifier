using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveExtractorEndToEndUnitTests
{
    [Fact]
    public void TryExtractArchiveStream_Succeeds_ForZipPayload()
    {
        using var scope = TestTempPaths.CreateScope("ftd-archive-extract");
        var destination = Path.Combine(scope.RootPath, "out");
        var payload = ArchiveEntryPayloadFactory.CreateZipWithSingleEntry("note.txt", 8);

        using var stream = new MemoryStream(payload, false);
        var opt = FileTypeProjectOptions.DefaultOptions();

        var ok = ArchiveExtractor.TryExtractArchiveStream(stream, destination, opt);

        Assert.True(ok);
        Assert.True(Directory.Exists(destination));
        Assert.True(File.Exists(Path.Combine(destination, "note.txt")));
    }
}