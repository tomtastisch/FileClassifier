using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveExtractionUnitTests
{
    [Fact]
    public void ExtractArchiveSafe_Succeeds_ForValidArchive_WithVerification()
    {
        var source = TestResources.Resolve("sample.zip");
        using var tempRoot = TestTempPaths.CreateScope("ftd-extract-test");
        var destination = Path.Combine(tempRoot.RootPath, "out");

        var ok = new FileTypeDetector().ExtractArchiveSafe(source, destination, true);

        Assert.True(ok);
        Assert.True(File.Exists(Path.Combine(destination, "note.txt")));
    }

    [Fact]
    public void ExtractArchiveSafe_FailsClosed_ForTraversalEntry()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-extract-test");
        var zipPath = Path.Combine(tempRoot.RootPath, "traversal.zip");
        var destination = Path.Combine(tempRoot.RootPath, "out");
        File.WriteAllBytes(zipPath, ArchiveEntryPayloadFactory.CreateZipWithSingleEntry("../evil.txt", 8));

        var ok = new FileTypeDetector().ExtractArchiveSafe(zipPath, destination, false);

        Assert.False(ok);
        Assert.False(Directory.Exists(destination));
        Assert.False(File.Exists(Path.Combine(tempRoot.RootPath, "evil.txt")));
    }

    [Fact]
    public void ExtractArchiveSafe_Fails_WhenDestinationAlreadyExists()
    {
        var source = TestResources.Resolve("sample.zip");
        using var tempRoot = TestTempPaths.CreateScope("ftd-extract-test");
        var destination = Path.Combine(tempRoot.RootPath, "out");
        Directory.CreateDirectory(destination);

        var ok = new FileTypeDetector().ExtractArchiveSafe(source, destination, false);

        Assert.False(ok);
    }

    [Fact]
    public void ExtractArchiveSafe_Fails_PreVerification_ForNonArchiveInput()
    {
        var source = TestResources.Resolve("sample.pdf");
        using var tempRoot = TestTempPaths.CreateScope("ftd-extract-test");
        var destination = Path.Combine(tempRoot.RootPath, "out");

        var ok = new FileTypeDetector().ExtractArchiveSafe(source, destination, true);

        Assert.False(ok);
        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public void ExtractArchiveSafe_Fails_ForRootDestinationPath()
    {
        var source = TestResources.Resolve("sample.zip");
        var rootPath = Path.GetPathRoot(Path.GetTempPath());
        Assert.False(string.IsNullOrWhiteSpace(rootPath));

        var ok = new FileTypeDetector().ExtractArchiveSafe(source, rootPath, false);
        Assert.False(ok);
    }

    [Fact]
    public void ExtractArchiveSafeToMemory_Succeeds_ForValidArchive_WithVerification()
    {
        var source = TestResources.Resolve("sample.zip");
        var entries = new FileTypeDetector().ExtractArchiveSafeToMemory(source, true);

        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal("note.txt", entries[0].RelativePath);
        Assert.NotEmpty(entries[0].Content);
    }

    [Fact]
    public void ExtractArchiveSafeToMemory_FailsClosed_ForTraversalEntry()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-extract-test");
        var zipPath = Path.Combine(tempRoot.RootPath, "traversal.zip");
        File.WriteAllBytes(zipPath, ArchiveEntryPayloadFactory.CreateZipWithSingleEntry("../evil.txt", 8));

        var entries = new FileTypeDetector().ExtractArchiveSafeToMemory(zipPath, false);
        Assert.NotNull(entries);
        Assert.Empty(entries);
        Assert.False(File.Exists(Path.Combine(tempRoot.RootPath, "evil.txt")));
    }
}