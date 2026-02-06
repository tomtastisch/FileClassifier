using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DeterministicHashingPrivateBranchUnitTests
{
    [Fact]
    public void NormalizeLabel_FallsBack_ForNullOrWhitespace()
    {
        var method =
            typeof(DeterministicHashing).GetMethod("NormalizeLabel", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var label1 = TestGuard.NotNull(method!.Invoke(null, new object?[] { null }) as string);
        var label2 = TestGuard.NotNull(method.Invoke(null, new object?[] { "   " }) as string);

        Assert.Equal("payload.bin", label1);
        Assert.Equal("payload.bin", label2);
    }

    [Fact]
    public void CopyBytes_ReturnsEmpty_ForNullOrEmpty()
    {
        var method = typeof(DeterministicHashing).GetMethod("CopyBytes", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var empty1 = TestGuard.NotNull(method!.Invoke(null, new object?[] { null }) as byte[]);
        var empty2 = TestGuard.NotNull(method.Invoke(null, new object?[] { Array.Empty<byte>() }) as byte[]);

        Assert.Empty(empty1);
        Assert.Empty(empty2);
    }

    [Fact]
    public void ComputeFastHash_ReturnsEmpty_WhenOptionDisabled()
    {
        var method =
            typeof(DeterministicHashing).GetMethod("ComputeFastHash", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var options = new DeterministicHashOptions { IncludeFastHash = false };
        var result = TestGuard.NotNull(method!.Invoke(null, new object?[] { new byte[] { 1, 2, 3 }, options }) as string);

        Assert.Equal(string.Empty, result);
    }
}
