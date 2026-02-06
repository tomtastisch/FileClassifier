using System;
using System.IO;
using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveInternalsPrivateBranchUnitTests
{
    private sealed class FakeEntry : IArchiveEntryModel
    {
        public string RelativePath { get; set; } = "entry.bin";
        public bool IsDirectory { get; set; }
        public long? UncompressedSize { get; set; }
        public long? CompressedSize { get; set; }
        public string LinkTarget { get; set; } = string.Empty;
        public Stream OpenStream() => Stream.Null;
    }

    private sealed class SizedEntry : IArchiveEntryModel
    {
        private readonly byte[] _payload;

        public SizedEntry(int size)
        {
            _payload = new byte[size];
        }

        public string RelativePath => "payload.bin";
        public bool IsDirectory => false;
        public long? UncompressedSize => null;
        public long? CompressedSize => null;
        public string LinkTarget => string.Empty;
        public Stream OpenStream() => new MemoryStream(_payload, writable: false);
    }

    [Fact]
    public void TryGetValidatedSize_MeasuresWhenUnknownAndRequired()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryGetValidatedSize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.AllowUnknownArchiveEntrySize = false;
        opt.MaxZipEntryUncompressedBytes = 10;

        object[] args = { new SizedEntry(3), opt, 0L, true };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.True(ok);
        Assert.Equal(3L, (long)args[2]);
    }

    [Fact]
    public void TryGetValidatedSize_FailsWhenMeasuredExceedsLimit()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryGetValidatedSize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.AllowUnknownArchiveEntrySize = false;
        opt.MaxZipEntryUncompressedBytes = 4;

        object[] args = { new SizedEntry(10), opt, 0L, true };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.False(ok);
    }

    [Fact]
    public void TryGetValidatedSize_RejectsNullInputs()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryGetValidatedSize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();

        object[] args1 = { null!, opt, 0L, false };
        object[] args2 = { new FakeEntry(), null!, 0L, false };

        Assert.False((bool)method!.Invoke(null, args1)!);
        Assert.False((bool)method!.Invoke(null, args2)!);
    }

    [Fact]
    public void TryGetValidatedSize_ReturnsTrue_WhenUnknownSizeNotRequired()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryGetValidatedSize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.AllowUnknownArchiveEntrySize = false;

        object[] args = { new FakeEntry { UncompressedSize = null }, opt, 0L, false };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.True(ok);
    }

    [Fact]
    public void TryMeasureEntrySize_ReturnsTrue_WhenAllowUnknownEnabled()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryMeasureEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.AllowUnknownArchiveEntrySize = true;

        object[] args = { new FakeEntry(), opt, 0L };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.True(ok);
    }

    [Fact]
    public void TryGetValidatedSize_ReturnsTrue_ForNegativeSize_WhenNotRequired()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryGetValidatedSize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();

        object[] args = { new FakeEntry { UncompressedSize = -1 }, opt, 0L, false };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.True(ok);
    }

    [Fact]
    public void TryMeasureEntrySize_ReturnsFalse_ForNullInputs()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryMeasureEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();

        object[] args1 = { null!, opt, 0L };
        object[] args2 = { new FakeEntry(), null!, 0L };

        Assert.False((bool)method!.Invoke(null, args1)!);
        Assert.False((bool)method.Invoke(null, args2)!);
    }

    [Fact]
    public void TryMeasureEntrySize_ReturnsFalse_WhenStreamUnreadable()
    {
        var method = typeof(SharpCompressArchiveBackend).GetMethod("TryMeasureEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();

        object[] args = { new UnreadableEntry(), opt, 0L };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.False(ok);
    }

    private sealed class UnreadableEntry : IArchiveEntryModel
    {
        public string RelativePath => "payload.bin";
        public bool IsDirectory => false;
        public long? UncompressedSize => null;
        public long? CompressedSize => null;
        public string LinkTarget => string.Empty;
        public Stream OpenStream() => new UnreadableStream();
    }

    private sealed class UnreadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }

    [Fact]
    public void SharpCompressBackend_ProcessesNestedGZipWithZipPayload()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipNestedBytes = 1024 * 10;
        opt.MaxZipNestingDepth = 2;

        var nestedZip = ArchiveEntryPayloadFactory.CreateZipWithEntries(1, 8);
        var gzipPayload = ArchivePayloadFactory.CreateGZipWithSingleEntry("payload.zip", nestedZip);

        using var stream = new MemoryStream(gzipPayload, writable: false);
        var backend = new SharpCompressArchiveBackend();

        var ok = backend.Process(stream, opt, depth: 0, containerTypeValue: ArchiveContainerType.GZip, extractEntry: _ => true);

        Assert.True(ok);
    }
}
