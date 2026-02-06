using System.Reflection;
using FileTypeDetection;
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

        var label1 = (string)method!.Invoke(null, new object?[] { null });
        var label2 = (string)method.Invoke(null, new object?[] { "   " });

        Assert.Equal("payload.bin", label1);
        Assert.Equal("payload.bin", label2);
    }

    [Fact]
    public void CopyBytes_ReturnsEmpty_ForNullOrEmpty()
    {
        var method = typeof(DeterministicHashing).GetMethod("CopyBytes", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var empty1 = (byte[])method!.Invoke(null, new object?[] { null });
        var empty2 = (byte[])method.Invoke(null, new object?[] { Array.Empty<byte>() })!;

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
        var result = (string)method!.Invoke(null, new object?[] { new byte[] { 1, 2, 3 }, options });

        Assert.Equal(string.Empty, result);
    }
}
