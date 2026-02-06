using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveExtractorAdditionalUnitTests
{
    [Fact]
    public void TryExtractArchiveStreamToMemory_ReturnsEmpty_ForNonArchivePayload()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        using var ms = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }, false);

        var entries = ArchiveExtractor.TryExtractArchiveStreamToMemory(ms, opt);

        Assert.Empty(entries);
    }

    [Fact]
    public void TryExtractArchiveStreamToMemory_ReturnsEmpty_ForUnreadableStream()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        using var stream = new UnreadableStream();

        var entries = ArchiveExtractor.TryExtractArchiveStreamToMemory(stream, opt);

        Assert.Empty(entries);
    }

    [Fact]
    public void TryExtractArchiveStreamToMemory_ReturnsEmpty_ForUnreadableStream_WithDescriptor()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        using var stream = new UnreadableStream();
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);

        var entries = ArchiveExtractor.TryExtractArchiveStreamToMemory(stream, opt, descriptor);

        Assert.Empty(entries);
    }

    [Fact]
    public void TryExtractArchiveStreamToMemory_ReturnsEmpty_ForNullOptionsOrDescriptor()
    {
        var payload = ArchiveEntryPayloadFactory.CreateZipWithEntries(1, 4);
        using var stream = new MemoryStream(payload, false);
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);

        var nullOpt = ArchiveExtractor.TryExtractArchiveStreamToMemory(stream, null!, descriptor);
        var nullDescriptor =
            ArchiveExtractor.TryExtractArchiveStreamToMemory(stream, FileTypeProjectOptions.DefaultOptions(), null!);

        Assert.Empty(nullOpt);
        Assert.Empty(nullDescriptor);
    }

    [Fact]
    public void TryExtractArchiveStream_ReturnsFalse_WhenDestinationExists()
    {
        using var scope = TestTempPaths.CreateScope("ftd-extract-existing");
        var destination = Path.Combine(scope.RootPath, "out");
        Directory.CreateDirectory(destination);

        var payload = ArchiveEntryPayloadFactory.CreateZipWithSingleEntry("note.txt", 4);
        using var stream = new MemoryStream(payload, false);
        var opt = FileTypeProjectOptions.DefaultOptions();

        var ok = ArchiveExtractor.TryExtractArchiveStream(stream, destination, opt);

        Assert.False(ok);
    }

    [Fact]
    public void TryExtractArchiveStream_ReturnsFalse_ForInvalidDestinationPath()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var payload = ArchiveEntryPayloadFactory.CreateZipWithEntries(1, 4);
        using var stream = new MemoryStream(payload, false);

        var ok = ArchiveExtractor.TryExtractArchiveStream(stream, "bad\0path", opt);

        Assert.False(ok);
    }

    [Fact]
    public void TryExtractArchiveStream_ReturnsFalse_WhenStreamNotArchive()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }, false);
        using var scope = TestTempPaths.CreateScope("ftd-extract-nonarchive");
        var destination = Path.Combine(scope.RootPath, "out");

        var ok = ArchiveExtractor.TryExtractArchiveStream(stream, destination, opt);

        Assert.False(ok);
    }

    [Fact]
    public void TryExtractArchiveStream_ReturnsFalse_ForNullInputs()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var payload = ArchiveEntryPayloadFactory.CreateZipWithEntries(1, 4);
        using var stream = new MemoryStream(payload, false);
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);

        Assert.False(ArchiveExtractor.TryExtractArchiveStream(null!, "x", opt, descriptor));
        Assert.False(ArchiveExtractor.TryExtractArchiveStream(stream, "x", null!, descriptor));
        Assert.False(ArchiveExtractor.TryExtractArchiveStream(stream, "x", opt, null!));
        Assert.False(ArchiveExtractor.TryExtractArchiveStream(stream, "x", opt, ArchiveDescriptor.UnknownDescriptor()));
        Assert.False(ArchiveExtractor.TryExtractArchiveStream(stream, " ", opt, descriptor));
    }

    [Fact]
    public void TryExtractArchiveStreamToMemory_ReturnsEmpty_WhenProcessingFails()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntries = 0;
        var payload = ArchiveEntryPayloadFactory.CreateZipWithEntries(1, 4);
        using var stream = new MemoryStream(payload, false);

        var entries = ArchiveExtractor.TryExtractArchiveStreamToMemory(stream, opt);

        Assert.Empty(entries);
    }

    [Fact]
    public void ExtractEntryToMemory_ReturnsTrue_ForDirectoryEntry()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToMemory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<ZipExtractedEntry>();
        var entry = new FakeEntry { RelativePath = "folder/", IsDirectory = true };

        var ok = (bool)method!.Invoke(null, new object[] { entry, entries, opt })!;

        Assert.True(ok);
        Assert.Empty(entries);
    }

    [Fact]
    public void ExtractEntryToMemory_ReturnsFalse_ForNullEntriesOrOptions()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToMemory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entry = new FakeEntry();
        var okEntriesNull = (bool)method!.Invoke(null, new object?[] { entry, null, opt })!;
        var okOptNull = (bool)method.Invoke(null, new object?[] { entry, new List<ZipExtractedEntry>(), null })!;

        Assert.False(okEntriesNull);
        Assert.False(okOptNull);
    }

    [Fact]
    public void ExtractEntryToMemory_ReturnsFalse_ForNullEntry()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToMemory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<ZipExtractedEntry>();

        var ok = (bool)method!.Invoke(null, new object?[] { null, entries, opt })!;

        Assert.False(ok);
    }

    [Fact]
    public void ExtractEntryToMemory_ReturnsFalse_WhenEntryStreamNullOrUnreadable()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToMemory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<ZipExtractedEntry>();
        var entry = new NullStreamEntry { RelativePath = "file.bin", UncompressedSize = 1 };

        var ok = (bool)method!.Invoke(null, new object[] { entry, entries, opt })!;

        Assert.False(ok);
    }

    [Fact]
    public void ExtractEntryToMemory_ReturnsFalse_ForInvalidPath()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToMemory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var entries = new List<ZipExtractedEntry>();
        var entry = new FakeEntry { RelativePath = "../evil.txt" };

        var ok = (bool)method!.Invoke(null, new object[] { entry, entries, opt })!;

        Assert.False(ok);
    }

    [Fact]
    public void ExtractEntryToDirectory_ReturnsFalse_ForNullInputs()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToDirectory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        var okEntryNull = (bool)method!.Invoke(null, new object?[] { null, "x", opt })!;
        var okOptNull = (bool)method.Invoke(null, new object?[] { new FakeEntry(), "x", null })!;

        Assert.False(okEntryNull);
        Assert.False(okOptNull);
    }

    [Fact]
    public void ExtractEntryToDirectory_ReturnsFalse_WhenEntryTooLarge()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToDirectory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-extract-large");
        var prefix = Path.GetFullPath(scope.RootPath) + Path.DirectorySeparatorChar;
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntryUncompressedBytes = 1;

        var entry = new FakeEntry
        {
            RelativePath = "big.bin",
            UncompressedSize = 10
        };

        var ok = (bool)method!.Invoke(null, new object[] { entry, prefix, opt })!;

        Assert.False(ok);
    }

    private sealed class FakeEntry : IArchiveEntryModel
    {
        public string RelativePath { get; set; } = "entry.txt";
        public bool IsDirectory { get; set; }
        public long? UncompressedSize { get; set; }
        public long? CompressedSize { get; set; }
        public string LinkTarget { get; } = string.Empty;

        public Stream OpenStream()
        {
            return Stream.Null;
        }
    }

    private sealed class NullStreamEntry : IArchiveEntryModel
    {
        public string RelativePath { get; set; } = "entry.txt";
        public bool IsDirectory { get; set; }
        public long? UncompressedSize { get; set; }
        public long? CompressedSize { get; set; }
        public string LinkTarget { get; } = string.Empty;

        public Stream OpenStream()
        {
            return null!;
        }
    }

    private sealed class UnreadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set { }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}