using System.Reflection;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DeterministicHashingApiContractUnitTests
{
    [Fact]
    public void PublicStaticSurface_MatchesApprovedContract()
    {
        var methods = typeof(DeterministicHashing)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == typeof(DeterministicHashing))
            .Select(Describe)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            "HashBytes(Byte[]):DeterministicHashEvidence",
            "HashBytes(Byte[],String):DeterministicHashEvidence",
            "HashBytes(Byte[],String,DeterministicHashOptions):DeterministicHashEvidence",
            "HashEntries(IReadOnlyList`1):DeterministicHashEvidence",
            "HashEntries(IReadOnlyList`1,String):DeterministicHashEvidence",
            "HashEntries(IReadOnlyList`1,String,DeterministicHashOptions):DeterministicHashEvidence",
            "HashFile(String):DeterministicHashEvidence",
            "HashFile(String,DeterministicHashOptions):DeterministicHashEvidence",
            "VerifyRoundTrip(String):DeterministicHashRoundTripReport",
            "VerifyRoundTrip(String,DeterministicHashOptions):DeterministicHashRoundTripReport"
        }.OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, methods);
    }

    [Fact]
    public void HashFile_MissingPath_FailsClosedWithoutThrowing()
    {
        var evidence = DeterministicHashing.HashFile("/tmp/does-not-exist-__ftd__.bin");

        Assert.Equal(FileKind.Unknown, evidence.DetectedType.Kind);
        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.False(evidence.Digests.HasPhysicalHash);
    }

    private static string Describe(MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Select(p => p.ParameterType.Name)
            .ToArray();
        return $"{method.Name}({string.Join(",", parameters)}):{method.ReturnType.Name}";
    }
}