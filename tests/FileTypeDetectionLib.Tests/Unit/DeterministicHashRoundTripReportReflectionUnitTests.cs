using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DeterministicHashRoundTripReportReflectionUnitTests
{
    [Fact]
    public void EqualLogical_ReturnsFalse_WhenEvidenceNull()
    {
        var method = typeof(DeterministicHashRoundTripReport)
            .GetMethod("EqualLogical", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = TestGuard.Unbox<bool>(method.Invoke(null, new object?[] { null, null }));

        Assert.False(result);
    }

    [Fact]
    public void EqualPhysical_ReturnsFalse_WhenEvidenceNull()
    {
        var method = typeof(DeterministicHashRoundTripReport)
            .GetMethod("EqualPhysical", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = TestGuard.Unbox<bool>(method.Invoke(null, new object?[] { null, null }));

        Assert.False(result);
    }
}
