using System.Reflection;
using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class EvidenceHashingReflectionUnitTests
{
    [Fact]
    public void ResolveHashOptions_FallsBack_WhenProjectOptionsNull()
    {
        var method =
            typeof(EvidenceHashing).GetMethod("ResolveHashOptions", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var result = TestGuard.NotNull(method.Invoke(null, new object?[] { null, null }) as HashOptions);

        Assert.NotNull(result);
        Assert.Equal("deterministic-roundtrip.bin", result.MaterializedFileName);
    }
}
