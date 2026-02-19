using System.Reflection;
using FileTypeDetectionLib.Tests.Support;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveInternalsNestedBranchUnitTests
{
    [Fact]
    public void TryProcessNestedGArchive_ReturnsFalse_WhenNotGzip()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryProcessNestedGArchive",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<IArchiveEntry>();

        var handled = TestGuard.Unbox<bool>(method.Invoke(null,
            new object[] { entries, opt, 0, ArchiveContainerType.Zip, null!, false }));

        Assert.False(handled);
    }

    [Fact]
    public void TryProcessNestedGArchive_ReturnsFalse_WhenEntryCountNotOne()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryProcessNestedGArchive",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<IArchiveEntry>
        {
            CreateZipArchiveEntry("a.txt", new byte[] { 1 }),
            CreateZipArchiveEntry("b.txt", new byte[] { 2 })
        };

        var handled = TestGuard.Unbox<bool>(method.Invoke(null,
            new object[] { entries, opt, 0, ArchiveContainerType.GZip, null!, false }));

        Assert.False(handled);
    }

    [Fact]
    public void TryProcessNestedGArchive_ReturnsTrue_WithInvalidNestedDescriptor()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryProcessNestedGArchive",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<IArchiveEntry>
        {
            CreateZipArchiveEntry("payload.bin", new byte[] { 0x01, 0x02 })
        };

        var handled = TestGuard.Unbox<bool>(method.Invoke(null,
            new object[] { entries, opt, 0, ArchiveContainerType.GZip, null!, true }));

        Assert.True(handled);
    }

    [Fact]
    public void TryReadEntryPayloadBoundedWithOptions_ReturnsFalse_ForInvalidInputs()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod(
            "TryReadEntryPayloadBoundedWithOptions",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(IArchiveEntry), typeof(long), typeof(FileTypeProjectOptions), typeof(byte[]).MakeByRefType() },
            modifiers: null)!;
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        Assert.False(TestGuard.Unbox<bool>(method.Invoke(null, new object?[] { null, 10L, opt, null })));
        Assert.False(TestGuard.Unbox<bool>(method.Invoke(null,
            new object?[] { CreateZipArchiveEntry("a.txt", new byte[] { 1 }), 0L, opt, null })));
    }

    private static IArchiveEntry CreateZipArchiveEntry(string name, byte[] payload)
    {
        using var ms = new MemoryStream();
        using (var writer = SharpCompressApiCompat.OpenWriter(ms, ArchiveType.Zip, new WriterOptions(CompressionType.Deflate)))
        using (var data = new MemoryStream(payload, false))
        {
            writer.Write(name, data, DateTime.UnixEpoch);
        }

        ms.Position = 0;
        var archive = SharpCompressApiCompat.OpenZipArchive(ms);
        return archive.Entries.First();
    }
}
