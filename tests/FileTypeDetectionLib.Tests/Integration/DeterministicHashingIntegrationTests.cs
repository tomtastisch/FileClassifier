using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Integration;

public sealed class DeterministicHashingIntegrationTests
{
    [Fact]
    public void LogicalHash_IsStableAcrossArchiveTarAndTarGz_ForSameContent()
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

    [Theory]
    [InlineData("fx.sample_zip")]
    [InlineData("fx.sample_rar")]
    [InlineData("fx.sample_7z")]
    public void ArchivePipeline_PreservesCombinedAndPerFileHashes_AfterExtractByteMaterializeAndRecheck(string fixtureId)
    {
        var path = TestResources.Resolve(fixtureId);
        Assert.True(ArchiveProcessing.TryValidate(path));

        var extractedEntries = ArchiveProcessing.ExtractToMemory(path, verifyBeforeExtract: true);
        Assert.NotEmpty(extractedEntries);

        var archiveEvidence = DeterministicHashing.HashFile(path);
        var extractedCombinedEvidence = DeterministicHashing.HashEntries(extractedEntries, $"{fixtureId}-extracted");
        Assert.True(archiveEvidence.Digests.HasLogicalHash);
        Assert.True(extractedCombinedEvidence.Digests.HasLogicalHash);
        Assert.Equal(archiveEvidence.Digests.LogicalSha256, extractedCombinedEvidence.Digests.LogicalSha256);

        using var scope = TestTempPaths.CreateScope($"ftd-materialized-{fixtureId}");
        var originalPerFileLogical = new Dictionary<string, string>(StringComparer.Ordinal);
        var rematerializedEntries = new List<ZipExtractedEntry>();

        foreach (var entry in extractedEntries.OrderBy(e => e.RelativePath, StringComparer.Ordinal))
        {
            var entryBytes = entry.Content.ToArray();
            var fromBytes = DeterministicHashing.HashBytes(entryBytes, entry.RelativePath);
            Assert.True(fromBytes.Digests.HasLogicalHash);
            Assert.True(fromBytes.Digests.HasPhysicalHash);
            Assert.Equal(fromBytes.Digests.PhysicalSha256, fromBytes.Digests.LogicalSha256);
            originalPerFileLogical[entry.RelativePath] = fromBytes.Digests.LogicalSha256;

            var destinationPath = Path.Combine(scope.RootPath, ToPlatformRelativePath(entry.RelativePath));
            Assert.True(FileMaterializer.Persist(entryBytes, destinationPath, overwrite: false, secureExtract: false));

            var fromMaterializedFile = DeterministicHashing.HashFile(destinationPath);
            Assert.True(fromMaterializedFile.Digests.HasLogicalHash);
            Assert.True(fromMaterializedFile.Digests.HasPhysicalHash);
            Assert.Equal(fromBytes.Digests.LogicalSha256, fromMaterializedFile.Digests.LogicalSha256);
            Assert.Equal(fromBytes.Digests.PhysicalSha256, fromMaterializedFile.Digests.PhysicalSha256);

            rematerializedEntries.Add(new ZipExtractedEntry(entry.RelativePath, File.ReadAllBytes(destinationPath)));
        }

        var rematerializedCombinedEvidence = DeterministicHashing.HashEntries(rematerializedEntries, $"{fixtureId}-materialized");
        Assert.True(rematerializedCombinedEvidence.Digests.HasLogicalHash);
        Assert.Equal(extractedCombinedEvidence.Digests.LogicalSha256, rematerializedCombinedEvidence.Digests.LogicalSha256);
        Assert.Equal(archiveEvidence.Digests.LogicalSha256, rematerializedCombinedEvidence.Digests.LogicalSha256);

        foreach (var entry in rematerializedEntries.OrderBy(e => e.RelativePath, StringComparer.Ordinal))
        {
            var hashedEntry = DeterministicHashing.HashBytes(entry.Content.ToArray(), entry.RelativePath);
            Assert.True(originalPerFileLogical.TryGetValue(entry.RelativePath, out var expectedLogical));
            Assert.Equal(expectedLogical, hashedEntry.Digests.LogicalSha256);
        }
    }

    private static string ToPlatformRelativePath(string relativePath)
    {
        return relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
    }
}
