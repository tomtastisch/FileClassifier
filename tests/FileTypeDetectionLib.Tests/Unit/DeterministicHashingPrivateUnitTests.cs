using System;
using System.IO;
using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DeterministicHashingPrivateUnitTests
{
    [Fact]
    public void TryReadFileBounded_ReturnsFalse_ForMissingPathOrOptions()
    {
        var method = typeof(DeterministicHashing).GetMethod("TryReadFileBounded", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        byte[] bytes = Array.Empty<byte>();
        string error = string.Empty;

        object[] args1 = { string.Empty, FileTypeProjectOptions.DefaultOptions(), bytes, error };
        var ok1 = (bool)method!.Invoke(null, args1)!;
        Assert.False(ok1);

        object[] args2 = { "missing", null!, bytes, error };
        var ok2 = (bool)method.Invoke(null, args2)!;
        Assert.False(ok2);
    }

    [Fact]
    public void TryReadFileBounded_ReturnsFalse_WhenFileTooLarge()
    {
        var method = typeof(DeterministicHashing).GetMethod("TryReadFileBounded", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-hash-read");
        var path = Path.Combine(scope.RootPath, "big.bin");
        File.WriteAllBytes(path, new byte[10]);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxBytes = 4;

        object[] args = { path, opt, Array.Empty<byte>(), string.Empty };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.False(ok);
    }

    [Fact]
    public void HashEntries_ReturnsFailure_ForDuplicateNormalizedPath_AfterTrim()
    {
        var a = new ZipExtractedEntry("a.txt", new byte[] { 0x01 });
        var b = new ZipExtractedEntry("a.txt ", new byte[] { 0x02 });

        var evidence = DeterministicHashing.HashEntries(new[] { a, b }, "entries");

        Assert.False(evidence.Digests.HasLogicalHash);
    }
}
