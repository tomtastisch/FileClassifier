using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class StreamGuardUnitTests
{
    [Fact]
    public void IsReadable_ReturnsFalse_ForWriteOnlyFileStream()
    {
        using var scope = TestTempPaths.CreateScope("ftd-streamguard");
        var path = Path.Combine(scope.RootPath, "writeonly.bin");

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

        Assert.False(StreamGuard.IsReadable(fs));
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
}

