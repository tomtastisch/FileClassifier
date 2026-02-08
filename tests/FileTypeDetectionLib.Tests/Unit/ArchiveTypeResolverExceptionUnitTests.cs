using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveTypeResolverExceptionUnitTests
{
    [Fact]
    public void TryDescribeStream_ReturnsFalse_OnException()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        using var stream = new ExplodingSeekStream();
        var ok = ArchiveTypeResolver.TryDescribeStream(stream, opt, ref descriptor);

        Assert.False(ok);
        Assert.Equal(ArchiveContainerType.Unknown, descriptor.ContainerType);
    }

    private sealed class ExplodingSeekStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set => throw new InvalidOperationException("boom");
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
            throw new InvalidOperationException("boom");
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("boom");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("boom");
        }
    }
}