using System;
using System.IO;
using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeDetectorReflectionUnitTests
{
    [Fact]
    public void ReadHeader_ReturnsEmpty_ForNullStreamOrZeroLimits()
    {
        var method = typeof(FileTypeDetector).GetMethod("ReadHeader", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var empty1 = (byte[])method!.Invoke(null, new object?[] { null, 128, 1024L })!;
        Assert.Empty(empty1);

        using var scope = TestTempPaths.CreateScope("ftd-readheader");
        var path = Path.Combine(scope.RootPath, "empty.bin");
        File.WriteAllBytes(path, Array.Empty<byte>());
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        var empty2 = (byte[])method.Invoke(null, new object?[] { fs, 128, 0L })!;
        Assert.Empty(empty2);
    }

    [Fact]
    public void ReadHeader_UsesDefaultSniffBytes_AndTruncatesWhenShorter()
    {
        var method = typeof(FileTypeDetector).GetMethod("ReadHeader", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-readheader-short");
        var path = Path.Combine(scope.RootPath, "short.bin");
        File.WriteAllBytes(path, new byte[] { 0x01, 0x02 });

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var data = (byte[])method!.Invoke(null, new object?[] { fs, -1, 1024L })!;

        Assert.Equal(2, data.Length);
    }

    [Fact]
    public void ReadHeader_ReturnsEmpty_WhenLengthExceedsMaxBytes()
    {
        var method = typeof(FileTypeDetector).GetMethod("ReadHeader", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-readheader-max");
        var path = Path.Combine(scope.RootPath, "big.bin");
        File.WriteAllBytes(path, new byte[10]);

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var data = (byte[])method!.Invoke(null, new object?[] { fs, 4, 5L })!;

        Assert.Empty(data);
    }
}
