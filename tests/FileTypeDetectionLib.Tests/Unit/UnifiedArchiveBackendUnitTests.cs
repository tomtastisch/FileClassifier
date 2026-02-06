using System.Text;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class UnifiedArchiveBackendUnitTests
{
    public static TheoryData<string, byte[], string, string> GeneratedArchivePayloadCases()
    {
        return new TheoryData<string, byte[], string, string>
        {
            {
                "tar",
                ArchivePayloadFactory.CreateTarWithSingleEntry("inner/note.txt", "hello"),
                "inner/note.txt",
                "hello"
            },
            {
                "tar.gz",
                ArchivePayloadFactory.CreateTarGzWithSingleEntry("inner/note.txt", "hello"),
                "inner/note.txt",
                "hello"
            }
        };
    }

    [Theory]
    [MemberData(nameof(GeneratedArchivePayloadCases))]
    public void Detect_ReturnsArchive_ForGeneratedPayloads(string archiveType, byte[] payload, string expectedPath,
        string expectedContent)
    {
        var detected = new FileTypeDetector().Detect(payload);
        Assert.Equal(FileKind.Zip, detected.Kind);
        Assert.False(string.IsNullOrWhiteSpace(archiveType));
        Assert.False(string.IsNullOrWhiteSpace(expectedPath));
        Assert.False(string.IsNullOrWhiteSpace(expectedContent));
    }

    [Theory]
    [MemberData(nameof(GeneratedArchivePayloadCases))]
    public void ArchiveProcessing_TryExtractToMemory_ReadsGeneratedInnerEntries(string archiveType, byte[] payload,
        string expectedPath, string expectedContent)
    {
        var entries = ArchiveProcessing.TryExtractToMemory(payload);

        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal(expectedPath, entries[0].RelativePath);
        Assert.Equal(expectedContent, Encoding.UTF8.GetString(entries[0].Content.ToArray()));
        Assert.False(string.IsNullOrWhiteSpace(archiveType));
    }

    [Theory]
    [MemberData(nameof(GeneratedArchivePayloadCases))]
    public void ArchiveEntryCollector_TryCollectFromBytes_MatchesArchiveProcessingFacade(string archiveType,
        byte[] payload, string expectedPath, string expectedContent)
    {
        var options = FileTypeOptions.GetSnapshot();
        IReadOnlyList<ZipExtractedEntry> collected = Array.Empty<ZipExtractedEntry>();

        var ok = ArchiveEntryCollector.TryCollectFromBytes(payload, options, ref collected);
        var facade = ArchiveProcessing.TryExtractToMemory(payload);

        Assert.True(ok);
        Assert.Equal(facade.Count, collected.Count);
        Assert.Equal(expectedPath, facade[0].RelativePath);
        Assert.Equal(expectedPath, collected[0].RelativePath);
        Assert.Equal(expectedContent, Encoding.UTF8.GetString(collected[0].Content.ToArray()));
        Assert.Equal(facade[0].Content.ToArray(), collected[0].Content.ToArray());
        Assert.False(string.IsNullOrWhiteSpace(archiveType));
    }

    [Theory]
    [MemberData(nameof(GeneratedArchivePayloadCases))]
    public void FileMaterializer_Persist_ExtractsGeneratedArchive_WhenSecureExtractEnabled(string archiveType,
        byte[] payload, string expectedPath, string expectedContent)
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-unified-archive");
        var destination = Path.Combine(tempRoot.RootPath, "out");

        var ok = FileMaterializer.Persist(payload, destination, false, true);
        var expectedAbsolutePath = Path.Combine(destination, expectedPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(ok);
        Assert.True(File.Exists(expectedAbsolutePath));
        Assert.Equal(expectedContent, File.ReadAllText(expectedAbsolutePath));
        Assert.False(string.IsNullOrWhiteSpace(archiveType));
    }

    [Fact]
    public void FileMaterializer_Persist_FailsClosed_ForTarSymlink_WhenRejectArchiveLinksEnabled()
    {
        var original = FileTypeOptions.GetSnapshot();
        var options = FileTypeOptions.GetSnapshot();
        options.RejectArchiveLinks = true;
        FileTypeOptions.SetSnapshot(options);

        using var tempRoot = TestTempPaths.CreateScope("ftd-unified-archive");
        var destination = Path.Combine(tempRoot.RootPath, "out");
        var tarWithLink = ArchivePayloadFactory.CreateTarWithSymlink("safe.txt", "ok", "ln.txt", "safe.txt");

        try
        {
            var ok = FileMaterializer.Persist(tarWithLink, destination, false, true);
            Assert.False(ok);
            Assert.False(Directory.Exists(destination));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }
}
