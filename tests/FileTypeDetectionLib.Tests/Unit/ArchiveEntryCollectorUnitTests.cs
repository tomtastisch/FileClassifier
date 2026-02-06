using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveEntryCollectorUnitTests
{
    [Fact]
    public void TryCollectFromBytes_ReturnsFalse_ForInvalidInputs()
    {
        IReadOnlyList<ZipExtractedEntry> entries = Array.Empty<ZipExtractedEntry>();
        var opt = FileTypeProjectOptions.DefaultOptions();

        Assert.False(ArchiveEntryCollector.TryCollectFromBytes(null!, opt, ref entries));
        Assert.False(ArchiveEntryCollector.TryCollectFromBytes(Array.Empty<byte>(), opt, ref entries));
        Assert.False(ArchiveEntryCollector.TryCollectFromBytes(new byte[] { 1, 2, 3 }, null!, ref entries));
        Assert.Empty(entries);
    }

    [Fact]
    public void TryCollectFromBytes_ReturnsFalse_ForNonArchivePayload()
    {
        IReadOnlyList<ZipExtractedEntry> entries = Array.Empty<ZipExtractedEntry>();
        var opt = FileTypeProjectOptions.DefaultOptions();
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var ok = ArchiveEntryCollector.TryCollectFromBytes(payload, opt, ref entries);

        Assert.False(ok);
        Assert.Empty(entries);
    }

    [Fact]
    public void TryCollectFromFile_ReturnsFalse_ForMissingFile()
    {
        IReadOnlyList<ZipExtractedEntry> entries = Array.Empty<ZipExtractedEntry>();
        var opt = FileTypeProjectOptions.DefaultOptions();
        var missing = Path.Combine(Path.GetTempPath(), "ftd-missing-" + Guid.NewGuid().ToString("N") + ".zip");

        var ok = ArchiveEntryCollector.TryCollectFromFile(missing, opt, ref entries);

        Assert.False(ok);
        Assert.Empty(entries);
    }

    [Fact]
    public void TryCollectFromFile_ReturnsFalse_WhenOptionsNull()
    {
        IReadOnlyList<ZipExtractedEntry> entries = Array.Empty<ZipExtractedEntry>();
        var path = TestResources.Resolve("sample.zip");

        var ok = ArchiveEntryCollector.TryCollectFromFile(path, null!, ref entries);

        Assert.False(ok);
        Assert.Empty(entries);
    }

    [Fact]
    public void TryCollectFromFile_ReturnsTrue_ForZipFixture()
    {
        IReadOnlyList<ZipExtractedEntry> entries = Array.Empty<ZipExtractedEntry>();
        var opt = FileTypeProjectOptions.DefaultOptions();
        var path = TestResources.Resolve("sample.zip");

        var ok = ArchiveEntryCollector.TryCollectFromFile(path, opt, ref entries);

        Assert.True(ok);
        Assert.NotEmpty(entries);
    }

    [Fact]
    public void TryCollectFromFile_ReturnsFalse_WhenSafetyGateRejects()
    {
        IReadOnlyList<ZipExtractedEntry> entries = Array.Empty<ZipExtractedEntry>();
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntries = 0;
        var path = TestResources.Resolve("sample.zip");

        var ok = ArchiveEntryCollector.TryCollectFromFile(path, opt, ref entries);

        Assert.False(ok);
        Assert.Empty(entries);
    }

    [Fact]
    public void TryCollectFromBytes_ReturnsFalse_WhenSafetyGateFails()
    {
        IReadOnlyList<ZipExtractedEntry> entries = Array.Empty<ZipExtractedEntry>();
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntryUncompressedBytes = 1;

        var payload = ArchiveEntryPayloadFactory.CreateZipWithSingleEntry("note.txt", 8);
        var ok = ArchiveEntryCollector.TryCollectFromBytes(payload, opt, ref entries);

        Assert.False(ok);
        Assert.Empty(entries);
    }
}