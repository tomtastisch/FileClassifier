using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DeterministicHashingReflectionUnitTests
{
    [Fact]
    public void ResolveHashOptions_FallsBack_WhenProjectOptionsNull()
    {
        var method =
            typeof(DeterministicHashing).GetMethod("ResolveHashOptions", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = TestGuard.NotNull(method.Invoke(null, new object?[] { null, null }) as DeterministicHashOptions);

        Assert.NotNull(result);
        Assert.Equal("deterministic-roundtrip.bin", result.MaterializedFileName);
    }
}
