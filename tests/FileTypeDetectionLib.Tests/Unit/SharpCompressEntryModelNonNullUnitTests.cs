using System.Text;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class SharpCompressEntryModelNonNullUnitTests
{
    [Fact]
    public void Properties_ReturnValues_ForRealArchiveEntry()
    {
        var payload = CreateTarWithEntry("note.txt", "hello");
        using var stream = new MemoryStream(payload, false);
        using var archive = SharpCompressApiCompat.OpenArchive(stream);

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
        using (var writer = SharpCompressApiCompat.OpenWriter(ms, ArchiveType.Tar, new WriterOptions(CompressionType.None)))
        using (var data = new MemoryStream(Encoding.UTF8.GetBytes(content)))
        {
            writer.Write(name, data, DateTime.UnixEpoch);
        }

        return ms.ToArray();
    }
}
