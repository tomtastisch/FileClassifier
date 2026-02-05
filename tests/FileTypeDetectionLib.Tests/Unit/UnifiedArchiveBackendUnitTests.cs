using System;
using System.IO;
using System.Linq;
using System.Text;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class UnifiedArchiveBackendUnitTests
{
    [Fact]
    public void Detect_ReturnsZip_ForTarPayload()
    {
        var tar = ArchivePayloadFactory.CreateTarWithSingleEntry("note.txt", "hello");
        var detected = new FileTypeDetector().Detect(tar);
        Assert.Equal(FileKind.Zip, detected.Kind);
    }

    [Fact]
    public void Detect_ReturnsZip_ForTarGzPayload()
    {
        var tarGz = ArchivePayloadFactory.CreateTarGzWithSingleEntry("note.txt", "hello");
        var detected = new FileTypeDetector().Detect(tarGz);
        Assert.Equal(FileKind.Zip, detected.Kind);
    }

    [Fact]
    public void ZipProcessing_TryExtractToMemory_ReadsTarGzInnerEntries()
    {
        var tarGz = ArchivePayloadFactory.CreateTarGzWithSingleEntry("inner/note.txt", "hello");
        var entries = ZipProcessing.TryExtractToMemory(tarGz);

        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal("inner/note.txt", entries[0].RelativePath);
        Assert.Equal("hello", Encoding.UTF8.GetString(entries[0].Content.ToArray()));
    }

    [Fact]
    public void FileMaterializer_Persist_ExtractsTarGz_WhenSecureExtractEnabled()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-unified-archive");
        var destination = Path.Combine(tempRoot.RootPath, "out");
        var tarGz = ArchivePayloadFactory.CreateTarGzWithSingleEntry("inner/note.txt", "hello");

        var ok = FileMaterializer.Persist(tarGz, destination, overwrite: false, secureExtract: true);
        Assert.True(ok);
        Assert.True(File.Exists(Path.Combine(destination, "inner", "note.txt")));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(destination, "inner", "note.txt")));
    }

    [Fact]
    public void FileMaterializer_Persist_FailsClosed_ForTarSymlink_WhenRejectArchiveLinksEnabled()
    {
        var original = FileTypeOptions.GetSnapshot();
        var options = FileTypeOptions.GetSnapshot();
        options.RejectArchiveLinks = true;
        FileTypeOptions.SetSnapshot(options);

        using var tempRoot = TestTempPaths.CreateScope("ftd-unified-archive");
        var destination = Path.Combine(tempRoot.RootPath, "out");
        var tarWithLink = ArchivePayloadFactory.CreateTarWithSymlink("safe.txt", "ok", "ln.txt", "safe.txt");

        try
        {
            var ok = FileMaterializer.Persist(tarWithLink, destination, overwrite: false, secureExtract: true);
            Assert.False(ok);
            Assert.False(Directory.Exists(destination));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }
}
