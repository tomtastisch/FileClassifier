using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveTypeResolverAdditionalUnitTests
{
    [Fact]
    public void TryDescribeStream_ReturnsFalse_ForUnreadableStream()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        var ok = ArchiveTypeResolver.TryDescribeStream(new UnreadableStream(), opt, ref descriptor);

        Assert.False(ok);
        Assert.Equal(ArchiveContainerType.Unknown, descriptor.ContainerType);
    }

    [Fact]
    public void TryDescribeStream_ReturnsFalse_ForNullStream()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        var ok = ArchiveTypeResolver.TryDescribeStream(null!, opt, ref descriptor);

        Assert.False(ok);
        Assert.Equal(ArchiveContainerType.Unknown, descriptor.ContainerType);
    }

    [Fact]
    public void TryDescribeStream_ReturnsFalse_ForNonArchivePayload()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }, false);
        var ok = ArchiveTypeResolver.TryDescribeStream(stream, opt, ref descriptor);

        Assert.False(ok);
        Assert.Equal(ArchiveContainerType.Unknown, descriptor.ContainerType);
    }

    [Fact]
    public void TryDescribeStream_ResetsPosition_ForSeekableStream()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();
        var payload = ArchivePayloadFactory.CreateTarWithSingleEntry("note.txt", "hi");

        using var stream = new MemoryStream(payload, false);
        stream.Position = 5;
        var ok = ArchiveTypeResolver.TryDescribeStream(stream, opt, ref descriptor);

        Assert.True(ok);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void TryDescribeBytes_ReturnsFalse_ForNullOrEmptyPayload()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        Assert.False(ArchiveTypeResolver.TryDescribeBytes(null!, opt, ref descriptor));
        Assert.False(ArchiveTypeResolver.TryDescribeBytes(Array.Empty<byte>(), opt, ref descriptor));
    }

    private sealed class UnreadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set { }
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
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}