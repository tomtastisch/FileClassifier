using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveSharpCompressCompatUnitTests
{
    [Fact]
    public void OpenArchive_ReturnsNull_ForNonArchivePayload()
    {
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }, false);
        var archive = ArchiveSharpCompressCompat.OpenArchive(stream);
        Assert.Null(archive);
    }

    [Fact]
    public void OpenArchive_ReturnsArchive_ForTarPayload()
    {
        var tar = ArchivePayloadFactory.CreateTarWithSingleEntry("note.txt", "ok");
        using var stream = new MemoryStream(tar, false);
        using var archive = ArchiveSharpCompressCompat.OpenArchive(stream);
        Assert.NotNull(archive);
    }

    [Fact]
    public void OpenArchiveForContainer_ReturnsArchive_ForGZipPayload()
    {
        var gzip = ArchivePayloadFactory.CreateGZipWithSingleEntry("payload.bin", new byte[] { 0x11, 0x22, 0x33 });
        using var stream = new MemoryStream(gzip, false);
        using var archive = ArchiveSharpCompressCompat.OpenArchiveForContainer(stream, ArchiveContainerType.GZip);
        Assert.NotNull(archive);
    }

    [Fact]
    public void HasGZipMagic_ReturnsTrue_ForGZipHeader()
    {
        using var stream = new MemoryStream(new byte[] { 0x1F, 0x8B, 0x08 }, false);
        Assert.True(ArchiveSharpCompressCompat.HasGZipMagic(stream));
    }

    [Fact]
    public void HasGZipMagic_ReturnsFalse_ForNonSeekableStream()
    {
        using var nonSeekable = new NonSeekableStream(new byte[] { 0x1F, 0x8B, 0x08 });
        Assert.False(ArchiveSharpCompressCompat.HasGZipMagic(nonSeekable));
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        internal NonSeekableStream(byte[] buffer) : base(buffer, false)
        {
        }

        public override bool CanSeek => false;
    }
}
