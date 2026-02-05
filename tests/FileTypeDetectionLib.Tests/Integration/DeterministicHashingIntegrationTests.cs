using System.Collections.Generic;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Integration;

public sealed class DeterministicHashingIntegrationTests
{
    [Fact]
    public void LogicalHash_IsStableAcrossZipTarAndTarGz_ForSameContent()
    {
        var zip = ArchivePayloadFactory.CreateZipWithSingleEntry("inner/note.txt", "hello");
        var tar = ArchivePayloadFactory.CreateTarWithSingleEntry("inner/note.txt", "hello");
        var tarGz = ArchivePayloadFactory.CreateTarGzWithSingleEntry("inner/note.txt", "hello");

        var zipEvidence = DeterministicHashing.HashBytes(zip, "sample.zip");
        var tarEvidence = DeterministicHashing.HashBytes(tar, "sample.tar");
        var tarGzEvidence = DeterministicHashing.HashBytes(tarGz, "sample.tar.gz");

        Assert.Equal(zipEvidence.Digests.LogicalSha256, tarEvidence.Digests.LogicalSha256);
        Assert.Equal(zipEvidence.Digests.LogicalSha256, tarGzEvidence.Digests.LogicalSha256);
        Assert.NotEqual(zipEvidence.Digests.PhysicalSha256, tarEvidence.Digests.PhysicalSha256);
    }

    [Theory]
    [InlineData("fx.sample_zip", true)]
    [InlineData("fx.sample_7z", true)]
    [InlineData("fx.sample_rar", true)]
    [InlineData("fx.sample_pdf", false)]
    public void VerifyRoundTrip_ProducesLogicalConsistency(string fixtureId, bool expectedArchive)
    {
        var path = TestResources.Resolve(fixtureId);
        var report = DeterministicHashing.VerifyRoundTrip(path);

        Assert.Equal(expectedArchive, report.IsArchiveInput);
        Assert.True(report.LogicalConsistent);
        Assert.True(report.LogicalH1EqualsH2);
        Assert.True(report.LogicalH1EqualsH3);
        Assert.True(report.LogicalH1EqualsH4);
    }

    [Fact]
    public void HashFile_Determinism_HoldsAcrossRepeatedFixtureRuns()
    {
        var fixtureIds = new List<string> { "fx.sample_zip", "fx.sample_7z", "fx.sample_rar", "fx.sample_pdf" };

        foreach (var fixtureId in fixtureIds)
        {
            var path = TestResources.Resolve(fixtureId);
            var first = DeterministicHashing.HashFile(path);
            var second = DeterministicHashing.HashFile(path);

            Assert.Equal(first.Digests.LogicalSha256, second.Digests.LogicalSha256);
            Assert.Equal(first.Digests.PhysicalSha256, second.Digests.PhysicalSha256);
            Assert.Equal(first.Digests.FastLogicalXxHash3, second.Digests.FastLogicalXxHash3);
        }
    }
}
