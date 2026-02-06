using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveInternalsNestedBranchUnitTests
{
    [Fact]
    public void TryProcessNestedGArchive_ReturnsFalse_WhenNotGzip()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryProcessNestedGArchive",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<IArchiveEntry>();
        var nestedResult = false;

        var handled = TestGuard.Unbox<bool>(method!.Invoke(null,
            new object[] { entries, opt, 0, ArchiveContainerType.Zip, null!, nestedResult }));

        Assert.False(handled);
    }

    [Fact]
    public void TryProcessNestedGArchive_ReturnsFalse_WhenEntryCountNotOne()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryProcessNestedGArchive",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<IArchiveEntry>();
        entries.Add(CreateZipArchiveEntry("a.txt", new byte[] { 1 }));
        entries.Add(CreateZipArchiveEntry("b.txt", new byte[] { 2 }));
        var nestedResult = false;

        var handled = TestGuard.Unbox<bool>(method!.Invoke(null,
            new object[] { entries, opt, 0, ArchiveContainerType.GZip, null!, nestedResult }));

        Assert.False(handled);
    }

    [Fact]
    public void TryProcessNestedGArchive_ReturnsTrue_WithInvalidNestedDescriptor()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryProcessNestedGArchive",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<IArchiveEntry>();
        entries.Add(CreateZipArchiveEntry("payload.bin", new byte[] { 0x01, 0x02 }));
        var nestedResult = true;

        var handled = TestGuard.Unbox<bool>(method!.Invoke(null,
            new object[] { entries, opt, 0, ArchiveContainerType.GZip, null!, nestedResult }));

        Assert.True(handled);
        Assert.True(nestedResult);
    }

    [Fact]
    public void TryReadEntryPayloadBounded_ReturnsFalse_ForInvalidInputs()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryReadEntryPayloadBounded",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        Assert.False(TestGuard.Unbox<bool>(method!.Invoke(null, new object?[] { null, 10L, null })));
        Assert.False(TestGuard.Unbox<bool>(method.Invoke(null,
            new object?[] { CreateZipArchiveEntry("a.txt", new byte[] { 1 }), 0L, null })));
    }

    private static IArchiveEntry CreateZipArchiveEntry(string name, byte[] payload)
    {
        using var ms = new MemoryStream();
        using (var writer = WriterFactory.Open(ms, ArchiveType.Zip, new WriterOptions(CompressionType.Deflate)))
        using (var data = new MemoryStream(payload, false))
        {
            writer.Write(name, data, DateTime.UnixEpoch);
        }

        ms.Position = 0;
        var archive = ZipArchive.Open(ms);
        return archive.Entries.First();
    }
}
