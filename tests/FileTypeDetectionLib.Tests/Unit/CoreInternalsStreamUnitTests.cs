using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class CoreInternalsStreamUnitTests
{
    [Fact]
    public void ArchiveSafetyGate_ReturnsFalse_ForUnreadableStream()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);

        var ok = ArchiveSafetyGate.IsArchiveSafeStream(new UnreadableStream(), opt, descriptor, depth: 0);

        Assert.False(ok);
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