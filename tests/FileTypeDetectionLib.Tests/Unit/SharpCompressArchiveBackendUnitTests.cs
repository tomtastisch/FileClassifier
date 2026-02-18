using FileTypeDetectionLib.Tests.Support;
using SharpCompress.Common;
using SharpCompress.Writers;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class SharpCompressArchiveBackendUnitTests
{
    [Fact]
    public void Process_ReturnsFalse_ForInvalidInputs()
    {
        var backend = new SharpCompressArchiveBackend();
        var opt = FileTypeProjectOptions.DefaultOptions();

        Assert.False(backend.Process(null!, opt, 0, ArchiveContainerType.Tar, _ => true));
        Assert.False(backend.Process(new MemoryStream(), null!, 0, ArchiveContainerType.Tar, _ => true));
        Assert.False(backend.Process(new MemoryStream(), opt, opt.MaxZipNestingDepth + 1, ArchiveContainerType.Tar,
            _ => true));
        Assert.False(backend.Process(new MemoryStream(), opt, 0, ArchiveContainerType.Unknown, _ => true));
    }

    [Fact]
    public void Process_ReturnsFalse_WhenArchiveTypeMismatch()
    {
        var backend = new SharpCompressArchiveBackend();
        var opt = FileTypeProjectOptions.DefaultOptions();
        var tar = CreateTarWithEntries(1, 4);

        using var stream = new MemoryStream(tar, false);
        var ok = backend.Process(stream, opt, 0, ArchiveContainerType.GZip, _ => true);

        Assert.False(ok);
    }

    [Fact]
    public void Process_ReturnsFalse_WhenEntryCountExceedsLimit()
    {
        var backend = new SharpCompressArchiveBackend();
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntries = 1;

        var tar = CreateTarWithEntries(2, 4);
        using var stream = new MemoryStream(tar, false);

        var ok = backend.Process(stream, opt, 0, ArchiveContainerType.Tar, _ => true);

        Assert.False(ok);
    }

    [Fact]
    public void Process_ReturnsFalse_WhenLinkEntriesRejected()
    {
        var backend = new SharpCompressArchiveBackend();
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.RejectArchiveLinks = true;

        var tarWithLink = ArchivePayloadFactory.CreateTarWithSymlink("safe.txt", "ok", "ln.txt", "safe.txt");
        using var stream = new MemoryStream(tarWithLink, false);

        var ok = backend.Process(stream, opt, 0, ArchiveContainerType.Tar, _ => true);

        Assert.False(ok);
    }

    [Fact]
    public void Process_ReturnsFalse_WhenEntrySizeExceedsLimit()
    {
        var backend = new SharpCompressArchiveBackend();
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntryUncompressedBytes = 1;

        var tar = CreateTarWithEntries(1, 8);
        using var stream = new MemoryStream(tar, false);

        var ok = backend.Process(stream, opt, 0, ArchiveContainerType.Tar, _ => true);

        Assert.False(ok);
    }

    [Fact]
    public void Process_ReturnsTrue_ForValidTar()
    {
        var backend = new SharpCompressArchiveBackend();
        var opt = FileTypeProjectOptions.DefaultOptions();

        var tar = CreateTarWithEntries(1, 4);
        using var stream = new MemoryStream(tar, false);

        var ok = backend.Process(stream, opt, 0, ArchiveContainerType.Tar, _ => true);

        Assert.True(ok);
    }

    [Fact]
    public void Process_ReturnsFalse_WhenExtractorReturnsFalse()
    {
        var backend = new SharpCompressArchiveBackend();
        var opt = FileTypeProjectOptions.DefaultOptions();

        var tar = CreateTarWithEntries(1, 4);
        using var stream = new MemoryStream(tar, false);

        var ok = backend.Process(stream, opt, 0, ArchiveContainerType.Tar, _ => false);

        Assert.False(ok);
    }

    private static byte[] CreateTarWithEntries(int entryCount, int entrySize)
    {
        using var ms = new MemoryStream();
        using (var writer = WriterFactory.OpenWriter(ms, ArchiveType.Tar, new WriterOptions(CompressionType.None)))
        {
            for (var i = 0; i < entryCount; i++)
            {
                var name = $"entry_{i}.txt";
                var payload = new byte[Math.Max(0, entrySize)];
                for (var p = 0; p < payload.Length; p++) payload[p] = (byte)('A' + i % 20);

                using var data = new MemoryStream(payload, false);
                writer.Write(name, data, DateTime.UnixEpoch);
            }
        }

        return ms.ToArray();
    }
}
