using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class HashEvidenceUnitTests
{
    [Fact]
    public void Constructor_SetsSafeDefaults_WhenInputsNull()
    {
        var evidence = new HashEvidence(
            sourceType: HashSourceType.Unknown,
            label: null,
            detectedType: null,
            entry: null,
            compressedBytes: null,
            uncompressedBytes: null,
            entryCount: -1,
            totalUncompressedBytes: -5,
            digests: null,
            notes: null);

        Assert.Equal(string.Empty, evidence.Label);
        Assert.Equal(FileKind.Unknown, evidence.DetectedType.Kind);
        Assert.Null(evidence.Entry);
        Assert.Equal(0, evidence.EntryCount);
        Assert.Equal(0, evidence.TotalUncompressedBytes);
        Assert.True(evidence.CompressedBytes.IsDefaultOrEmpty);
        Assert.True(evidence.UncompressedBytes.IsDefaultOrEmpty);
        Assert.False(evidence.Digests.HasLogicalHash);
        Assert.False(evidence.Digests.HasPhysicalHash);
        Assert.Equal(string.Empty, evidence.Notes);
    }
}