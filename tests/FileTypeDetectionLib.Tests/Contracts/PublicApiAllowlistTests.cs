using System.Reflection;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Contracts;

[Trait("Category", "ApiContract")]
public sealed class PublicApiAllowlistTests
{
    private static readonly string[] AllowedPublicTypes =
    {
        "Tomtastisch.FileClassifier.ArchiveProcessing",
        "Tomtastisch.FileClassifier.DetectionDetail",
        "Tomtastisch.FileClassifier.EvidenceHashing",
        "Tomtastisch.FileClassifier.FileKind",
        "Tomtastisch.FileClassifier.FileMaterializer",
        "Tomtastisch.FileClassifier.FileType",
        "Tomtastisch.FileClassifier.FileTypeDetector",
        "Tomtastisch.FileClassifier.FileTypeOptions",
        "Tomtastisch.FileClassifier.FileTypeProjectBaseline",
        "Tomtastisch.FileClassifier.FileTypeProjectOptions",
        "Tomtastisch.FileClassifier.HashDigestSet",
        "Tomtastisch.FileClassifier.HashEvidence",
        "Tomtastisch.FileClassifier.HashOptions",
        "Tomtastisch.FileClassifier.HashRoundTripReport",
        "Tomtastisch.FileClassifier.HashRoundTripReport+HashSlot",
        "Tomtastisch.FileClassifier.HashSourceType",
        "Tomtastisch.FileClassifier.ZipExtractedEntry"
    };

    [Fact]
    public void PublicTypes_MatchExplicitAllowlist()
    {
        var assembly = typeof(FileTypeDetector).Assembly;
        var actual = assembly.GetTypes()
            .Where(type => (type.IsPublic || type.IsNestedPublic) &&
                           type.Namespace == "Tomtastisch.FileClassifier")
            .Select(type => type.FullName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var expected = AllowedPublicTypes
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }
}
