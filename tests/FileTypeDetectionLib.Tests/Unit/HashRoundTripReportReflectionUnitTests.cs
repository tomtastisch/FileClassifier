using System.Reflection;
using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class HashRoundTripReportReflectionUnitTests
{
    [Fact]
    public void EqualLogical_ReturnsFalse_WhenEvidenceNull()
    {
        var method = typeof(HashRoundTripReport)
            .GetMethod("EqualLogical", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = TestGuard.Unbox<bool>(method.Invoke(null, new object?[] { null, null }));

        Assert.False(result);
    }

    [Fact]
    public void EqualPhysical_ReturnsFalse_WhenEvidenceNull()
    {
        var method = typeof(HashRoundTripReport)
            .GetMethod("EqualPhysical", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.NotNull(method);
        var result = TestGuard.Unbox<bool>(method.Invoke(null, new object?[] { null, null }));

        Assert.False(result);
    }
}
