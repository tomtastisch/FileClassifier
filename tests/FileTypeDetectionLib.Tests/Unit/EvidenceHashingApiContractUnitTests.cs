using System.Reflection;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

[Trait("Category", "ApiContract")]
public sealed class EvidenceHashingApiContractUnitTests
{
    private static readonly string[] sourceArray = new[]
        {
            "HashBytes(Byte[]):HashEvidence",
            "HashBytes(Byte[],String):HashEvidence",
            "HashBytes(Byte[],String,HashOptions):HashEvidence",
            "HashEntries(IReadOnlyList`1):HashEvidence",
            "HashEntries(IReadOnlyList`1,String):HashEvidence",
            "HashEntries(IReadOnlyList`1,String,HashOptions):HashEvidence",
            "HashFile(String):HashEvidence",
            "HashFile(String,HashOptions):HashEvidence",
            "VerifyRoundTrip(String):HashRoundTripReport",
            "VerifyRoundTrip(String,HashOptions):HashRoundTripReport"
        };

    [Fact]
    public void PublicStaticSurface_MatchesApprovedContract()
    {
        var methods = typeof(EvidenceHashing)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == typeof(EvidenceHashing))
            .Select(Describe)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var expected = sourceArray.OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, methods);
    }

    [Fact]
    public void HashFile_MissingPath_FailsClosedWithoutThrowing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.bin");
        var evidence = EvidenceHashing.HashFile(missingPath);

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
