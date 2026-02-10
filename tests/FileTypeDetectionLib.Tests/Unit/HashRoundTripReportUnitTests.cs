using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class HashRoundTripReportUnitTests
{
    [Fact]
    public void Constructor_DefaultsToFailureEvidence_WhenInputsNull()
    {
        var report = new HashRoundTripReport("", isArchiveInput: false, h1: null, h2: null, h3: null,
            h4: null, notes: null);

        Assert.False(report.LogicalConsistent);
        Assert.False(report.LogicalH1EqualsH2);
        Assert.False(report.LogicalH1EqualsH3);
        Assert.False(report.LogicalH1EqualsH4);
        Assert.False(report.PhysicalH1EqualsH2);
        Assert.False(report.PhysicalH1EqualsH3);
        Assert.False(report.PhysicalH1EqualsH4);
    }

    [Fact]
    public void Constructor_ReportsConsistency_WhenLogicalAndPhysicalMatch()
    {
        var digest = new HashDigestSet(
            physicalSha256: "a",
            logicalSha256: "b",
            fastPhysicalXxHash3: string.Empty,
            fastLogicalXxHash3: string.Empty,
            hmacPhysicalSha256: string.Empty,
            hmacLogicalSha256: string.Empty,
            hasPhysicalHash: true,
            hasLogicalHash: true);

        var evidence = new HashEvidence(
            sourceType: HashSourceType.RawBytes,
            label: "x",
            detectedType: FileTypeRegistry.Resolve(FileKind.Unknown),
            entry: null,
            compressedBytes: new byte[] { 0x01 },
            uncompressedBytes: new byte[] { 0x01 },
            entryCount: 1,
            totalUncompressedBytes: 1,
            digests: digest,
            notes: "ok");

        var report = new HashRoundTripReport("x", isArchiveInput: false, h1: evidence, h2: evidence,
            h3: evidence, h4: evidence, notes: "ok");

        Assert.True(report.LogicalConsistent);
        Assert.True(report.LogicalH1EqualsH2);
        Assert.True(report.LogicalH1EqualsH3);
        Assert.True(report.LogicalH1EqualsH4);
        Assert.True(report.PhysicalH1EqualsH2);
        Assert.True(report.PhysicalH1EqualsH3);
        Assert.True(report.PhysicalH1EqualsH4);
    }

    [Fact]
    public void Constructor_DistinguishesPhysicalWhenLogicalMissing()
    {
        var digest = new HashDigestSet(
            physicalSha256: "abc",
            logicalSha256: string.Empty,
            fastPhysicalXxHash3: string.Empty,
            fastLogicalXxHash3: string.Empty,
            hmacPhysicalSha256: string.Empty,
            hmacLogicalSha256: string.Empty,
            hasPhysicalHash: true,
            hasLogicalHash: false);

        var evidence = new HashEvidence(
            sourceType: HashSourceType.RawBytes,
            label: "x",
            detectedType: FileTypeRegistry.Resolve(FileKind.Unknown),
            entry: null,
            compressedBytes: new byte[] { 0x01 },
            uncompressedBytes: new byte[] { 0x01 },
            entryCount: 1,
            totalUncompressedBytes: 1,
            digests: digest,
            notes: "ok");

        var report = new HashRoundTripReport("x", isArchiveInput: false, h1: evidence, h2: evidence,
            h3: evidence, h4: evidence, notes: "ok");

        Assert.False(report.LogicalH1EqualsH2);
        Assert.True(report.PhysicalH1EqualsH2);
    }
}
