using System.Text;
using FileTypeDetection;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class SharpCompressEntryModelNonNullUnitTests
{
    [Fact]
    public void Properties_ReturnValues_ForRealArchiveEntry()
    {
        var payload = CreateTarWithEntry("note.txt", "hello");
        using var stream = new MemoryStream(payload, false);
        using var archive = ArchiveFactory.Open(stream);

        var entry = archive.Entries.First();
        var model = new SharpCompressEntryModel(entry);

        Assert.Equal("note.txt", model.RelativePath);
        Assert.False(model.IsDirectory);
        Assert.NotNull(model.UncompressedSize);
        Assert.NotNull(model.CompressedSize);
        Assert.Equal(string.Empty, model.LinkTarget);
        using var opened = model.OpenStream();
        Assert.True(opened.CanRead);
    }

    private static byte[] CreateTarWithEntry(string name, string content)
    {
        using var ms = new MemoryStream();
        using (var writer = WriterFactory.Open(ms, ArchiveType.Tar, new WriterOptions(CompressionType.None)))
        using (var data = new MemoryStream(Encoding.UTF8.GetBytes(content)))
        {
            writer.Write(name, data, DateTime.UnixEpoch);
        }

        return ms.ToArray();
    }
}
