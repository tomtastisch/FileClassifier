using System.IO.Compression;
using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveStreamEngineUnitTests
{
    [Fact]
    public void ValidateArchiveStream_ReturnsFalse_WhenDepthExceeded()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipNestingDepth = 0;
        var payload = CreateZipWithSingleEntry("a.txt", new byte[] { 0x01 });

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ValidateArchiveStream(stream, opt, depth: 1);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_Fails_WhenEntryCountExceeded()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntries = 1;
        var payload = CreateZipWithEntries(
            ("a.txt", new byte[] { 0x01 }),
            ("b.txt", new byte[] { 0x02 }));

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_Fails_WhenCompressionRatioExceeded()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipCompressionRatio = 1;
        var payload = CreateZipWithSingleEntry("a.txt", new byte[1024]);

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_Fails_WhenNestedEntryTooLarge()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipNestedBytes = 1;
        var nested = CreateZipWithSingleEntry("inner.txt", new byte[32]);
        var payload = CreateZipWithSingleEntry("inner.zip", nested);

        using var stream = new MemoryStream(payload, false);
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: null);

        Assert.False(ok);
    }

    [Fact]
    public void ProcessArchiveStream_InvokesExtractor_ForTopLevelEntries()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var payload = CreateZipWithSingleEntry("a.txt", new byte[] { 0x01, 0x02 });

        using var stream = new MemoryStream(payload, false);
        var saw = false;
        var ok = ArchiveStreamEngine.ProcessArchiveStream(stream, opt, depth: 0, extractEntry: entry =>
        {
            saw = true;
            Assert.Equal("a.txt", entry.FullName);
            return true;
        });

        Assert.True(ok);
        Assert.True(saw);
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