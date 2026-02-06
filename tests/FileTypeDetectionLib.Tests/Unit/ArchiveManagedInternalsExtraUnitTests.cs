using System.IO.Compression;
using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveManagedInternalsExtraUnitTests
{
    [Fact]
    public void ProcessArchiveStream_Fails_WhenTotalUncompressedBytesExceeded()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipTotalUncompressedBytes = 2;

        var payload = CreateZipWithEntries(("a.txt", new byte[] { 0x01, 0x02 }), ("b.txt", new byte[] { 0x03 }));
        using var stream = new MemoryStream(payload, false);

        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_Fails_WhenEntryExceedsMaxEntrySize()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntryUncompressedBytes = 2;

        var payload = CreateZipWithEntries(("a.txt", new byte[] { 0x01, 0x02, 0x03 }));
        using var stream = new MemoryStream(payload, false);

        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_Fails_WhenNestedArchiveDepthExceeded()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipNestingDepth = 0;
        opt.MaxZipNestedBytes = 1024 * 10;

        var nested = CreateZipWithEntries(("inner.txt", new byte[] { 0x01 }));
        var payload = CreateZipWithEntries(("inner.zip", nested));

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.False(ok);
    }

    private static byte[] CreateZipWithEntries(params (string name, byte[] content)[] entries)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = zip.CreateEntry(name, CompressionLevel.SmallestSize);
                using var s = entry.Open();
                if (content.Length > 0) s.Write(content, 0, content.Length);
            }
        }

        return ms.ToArray();
    }
}