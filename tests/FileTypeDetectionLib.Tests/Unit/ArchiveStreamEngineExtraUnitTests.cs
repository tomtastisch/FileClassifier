using System.IO.Compression;
using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveStreamEngineExtraUnitTests
{
    [Fact]
    public void ProcessArchiveStream_Fails_WhenEntrySizeExceedsLimit()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntryUncompressedBytes = 1;
        var payload = CreateZipWithSingleEntry("a.txt", new byte[] { 0x01, 0x02, 0x03 });

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_Fails_WhenTotalUncompressedExceedsLimit()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipTotalUncompressedBytes = 2;
        var payload = CreateZipWithEntries(
            ("a.txt", new byte[] { 0x01, 0x02 }),
            ("b.txt", new byte[] { 0x03 }));

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_ReturnsFalse_WhenExtractorReturnsFalse()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var payload = CreateZipWithSingleEntry("a.txt", new byte[] { 0x01 });

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: _ => false);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_AllowsNestedArchive_WhenWithinLimits()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipNestedBytes = 1024 * 10;
        opt.MaxZipNestingDepth = 2;

        var nested = CreateZipWithSingleEntry("inner.txt", new byte[] { 0x01, 0x02 });
        var payload = CreateZipWithSingleEntry("inner.zip", nested);

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.True(ok);
    }

    [Fact]
    public void ProcessArchiveStream_HandlesShortHeader_InNestedCheck()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var payload = CreateZipWithSingleEntry("tiny.bin", new byte[] { 0x01 });

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.True(ok);
    }

    private static byte[] CreateZipWithSingleEntry(string name, byte[] content)
    {
        return CreateZipWithEntries((name, content));
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