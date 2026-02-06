using System;
using System.IO;
using System.Text;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using Xunit;

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

        Assert.Equal((string?)"note.txt", (string?)model.RelativePath);
        Assert.False((bool)model.IsDirectory);
        Assert.NotNull<long>(model.UncompressedSize);
        Assert.NotNull<long>(model.CompressedSize);
        Assert.Equal(string.Empty, (string?)model.LinkTarget);
        using var opened = model.OpenStream();
        Assert.True((bool)opened.CanRead);
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