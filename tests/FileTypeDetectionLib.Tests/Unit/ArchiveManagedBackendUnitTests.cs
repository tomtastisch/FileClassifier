using System.IO;
using System.IO.Compression;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveManagedBackendUnitTests
{
    [Fact]
    public void Process_FailsForNonZipContainer()
    {
        var backend = new ArchiveManagedBackend();
        using var stream = new MemoryStream(new byte[0]);
        var opt = FileTypeProjectOptions.DefaultOptions();

        var ok = backend.Process(stream, opt, 0, ArchiveContainerType.Rar, null);

        Assert.False(ok);
    }

    [Fact]
    public void Process_ExtractsEntryModels_ForZip()
    {
        var backend = new ArchiveManagedBackend();
        var opt = FileTypeProjectOptions.DefaultOptions();
        var payload = CreateZipBytes();

        using var stream = new MemoryStream(payload);
        var sawFile = false;
        var sawDir = false;

        var ok = backend.Process(stream, opt, 0, ArchiveContainerType.Zip, entry =>
        {
            if (entry.RelativePath == "a.txt")
            {
                sawFile = true;
                Assert.False(entry.IsDirectory);
                Assert.Equal(3, entry.UncompressedSize);
                Assert.True(entry.CompressedSize.HasValue);
                using var s = entry.OpenStream();
                Assert.True(s.CanRead);
            }

            if (entry.RelativePath == "dir/")
            {
                sawDir = true;
                Assert.True(entry.IsDirectory);
            }

            return true;
        });

        Assert.True(ok);
        Assert.True(sawFile);
        Assert.True(sawDir);
    }

    private static byte[] CreateZipBytes()
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var file = zip.CreateEntry("a.txt");
            using (var s = file.Open())
            {
                var content = new byte[] { 0x01, 0x02, 0x03 };
                s.Write(content, 0, content.Length);
            }

            zip.CreateEntry("dir/");
        }

        return ms.ToArray();
    }
}