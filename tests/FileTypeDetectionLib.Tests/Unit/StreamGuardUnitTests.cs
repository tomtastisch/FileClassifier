using FileTypeDetectionLib.Tests.Support;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class StreamGuardUnitTests
{
    [Fact]
    public void IsReadable_ReturnsFalse_ForNullStream()
    {
        Assert.False(StreamGuard.IsReadable(null!));
    }

    [Fact]
    public void IsReadable_ReturnsFalse_ForWriteOnlyFileStream()
    {
        using var scope = TestTempPaths.CreateScope("ftd-streamguard");
        var path = Path.Combine(scope.RootPath, "writeonly.bin");

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

        Assert.False(StreamGuard.IsReadable(fs));
    }

    [Fact]
    public void IsReadable_ReturnsTrue_ForReadableStream()
    {
        using var ms = new MemoryStream(new byte[] { 0x01 }, writable: false);

        Assert.True(StreamGuard.IsReadable(ms));
    }

    [Fact]
    public void RewindToStart_ResetsPosition_ForSeekableStream()
    {
        using var ms = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 }, writable: false);
        ms.Position = 2;

        StreamGuard.RewindToStart(ms);

        Assert.Equal(0, ms.Position);
    }

    [Fact]
    public void RewindToStart_DoesNotThrow_ForNullStream()
    {
        StreamGuard.RewindToStart(null!);
    }

    [Fact]
    public void RewindToStart_DoesNotThrow_ForNonSeekableStream()
    {
        StreamGuard.RewindToStart(new NonSeekableReadableStream());
    }

    private sealed class NonSeekableReadableStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
