using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class OptionsPropagationCoreClassesUnitTests
{
    [Fact]
    public void FileTypeDetector_DetectBytes_UsesLoadedMaxBytes()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"maxBytes\":1}"));

            var detected = new FileTypeDetector().Detect(new byte[] { 0x01, 0x02 });

            Assert.Equal(FileKind.Unknown, detected.Kind);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void FileTypeDetector_DetectPath_UsesLoadedSniffBytes()
    {
        var original = FileTypeOptions.GetSnapshot();
        var samplePdf = TestResources.Resolve("sample.pdf");

        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"sniffBytes\":2}"));

            var detected = new FileTypeDetector().Detect(samplePdf);

            Assert.Equal(FileKind.Unknown, detected.Kind);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void ArchiveProcessing_TryValidatePath_UsesLoadedMaxZipEntries()
    {
        var original = FileTypeOptions.GetSnapshot();
        using var scope = TestTempPaths.CreateScope("ftd-options-prop-ap-path");
        var archivePath = Path.Combine(scope.RootPath, "payload.zip");
        File.WriteAllBytes(archivePath, ArchiveEntryPayloadFactory.CreateZipWithEntries(2, 4));

        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"maxZipEntries\":1}"));

            var ok = ArchiveProcessing.TryValidate(archivePath);

            Assert.False(ok);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void ArchiveProcessing_TryExtractToMemory_UsesLoadedMaxZipEntries()
    {
        var original = FileTypeOptions.GetSnapshot();
        var payload = ArchiveEntryPayloadFactory.CreateZipWithEntries(2, 4);

        try
        {
            var baseline = ArchiveProcessing.TryExtractToMemory(payload);
            Assert.Equal(2, baseline.Count);

            Assert.True(FileTypeOptions.LoadOptions("{\"maxZipEntries\":1}"));
            var reduced = ArchiveProcessing.TryExtractToMemory(payload);

            Assert.Empty(reduced);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void FileMaterializer_PersistRaw_UsesLoadedMaxBytes()
    {
        var original = FileTypeOptions.GetSnapshot();
        using var scope = TestTempPaths.CreateScope("ftd-options-prop-mat-raw");
        var destination = Path.Combine(scope.RootPath, "raw.bin");
        var payload = new byte[] { 0x01, 0x02, 0x03 };

        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"maxBytes\":1}"));

            var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: false);

            Assert.False(ok);
            Assert.False(File.Exists(destination));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void FileMaterializer_PersistSecureExtract_UsesLoadedMaxZipEntryUncompressedBytes()
    {
        var original = FileTypeOptions.GetSnapshot();
        using var scope = TestTempPaths.CreateScope("ftd-options-prop-mat-archive");
        var destination = Path.Combine(scope.RootPath, "out");
        var payload = ArchiveEntryPayloadFactory.CreateZipWithSingleEntry("note.txt", 8);

        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"maxZipEntryUncompressedBytes\":4}"));

            var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: true);

            Assert.False(ok);
            Assert.False(Directory.Exists(destination));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void EvidenceHashing_HashBytes_UsesLoadedIncludeFastHash()
    {
        var original = FileTypeOptions.GetSnapshot();
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"deterministicHashIncludeFastHash\":false}"));

            var evidence = EvidenceHashing.HashBytes(payload, "sample.pdf");

            Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.FastPhysicalXxHash3));
            Assert.True(string.IsNullOrWhiteSpace(evidence.Digests.FastLogicalXxHash3));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void EvidenceHashing_HashBytes_UsesLoadedIncludePayloadCopies()
    {
        var original = FileTypeOptions.GetSnapshot();
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        try
        {
            Assert.True(FileTypeOptions.LoadOptions("{\"deterministicHashIncludePayloadCopies\":false}"));

            var evidence = EvidenceHashing.HashBytes(payload, "sample.pdf");

            Assert.True(evidence.CompressedBytes.IsDefaultOrEmpty);
            Assert.True(evidence.UncompressedBytes.IsDefaultOrEmpty);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }
}
