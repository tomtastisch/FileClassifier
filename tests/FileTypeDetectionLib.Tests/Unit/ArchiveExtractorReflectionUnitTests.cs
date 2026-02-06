using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveExtractorReflectionUnitTests
{
    [Fact]
    public void ExtractEntryToDirectory_CreatesDirectoryEntry()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToDirectory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-extract-dir");
        var prefix = Path.GetFullPath(scope.RootPath) + Path.DirectorySeparatorChar;

        var entry = new FakeEntry(() => Stream.Null)
        {
            RelativePath = "folder/",
            IsDirectory = true
        };

        var opt = FileTypeProjectOptions.DefaultOptions();
        var ok = (bool)method!.Invoke(null, new object[] { entry, prefix, opt })!;

        Assert.True(ok);
        Assert.True(Directory.Exists(Path.Combine(scope.RootPath, "folder")));
    }

    [Fact]
    public void ExtractEntryToDirectory_FailsForTraversalOrExistingTarget()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToDirectory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-extract-traversal");
        var prefix = Path.GetFullPath(scope.RootPath) + Path.DirectorySeparatorChar;
        var opt = FileTypeProjectOptions.DefaultOptions();

        var traversal = new FakeEntry(() => Stream.Null) { RelativePath = "../evil.txt" };
        Assert.False((bool)method!.Invoke(null, new object[] { traversal, prefix, opt })!);

        var existing = Path.Combine(scope.RootPath, "exists.txt");
        File.WriteAllText(existing, "x");

        var entry = new FakeEntry(() => new MemoryStream(new byte[] { 1 }))
            { RelativePath = "exists.txt", UncompressedSize = 1 };
        Assert.False((bool)method.Invoke(null, new object[] { entry, prefix, opt })!);
    }

    [Fact]
    public void ExtractEntryToDirectory_FailsWhenStreamUnreadable()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToDirectory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-extract-unreadable");
        var prefix = Path.GetFullPath(scope.RootPath) + Path.DirectorySeparatorChar;
        var opt = FileTypeProjectOptions.DefaultOptions();

        var entry = new FakeEntry(() => new UnreadableStream())
        {
            RelativePath = "file.bin",
            UncompressedSize = 1
        };

        Assert.False((bool)method!.Invoke(null, new object[] { entry, prefix, opt })!);
    }

    [Fact]
    public void ExtractEntryToMemory_ReturnsFalse_WhenEntryTooLarge()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ExtractEntryToMemory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntryUncompressedBytes = 2;

        var entry = new FakeEntry(() => new MemoryStream(new byte[10]))
        {
            RelativePath = "big.bin",
            UncompressedSize = 10
        };

        var list = new List<ZipExtractedEntry>();
        var ok = (bool)method!.Invoke(null, new object[] { entry, list, opt })!;

        Assert.False(ok);
        Assert.Empty(list);
    }

    private sealed class FakeEntry : IArchiveEntryModel
    {
        private readonly Func<Stream> _streamFactory;

        public FakeEntry(Func<Stream> streamFactory)
        {
            _streamFactory = streamFactory;
        }

        public string RelativePath { get; set; } = "entry.txt";
        public bool IsDirectory { get; set; }
        public long? UncompressedSize { get; set; }
        public long? CompressedSize { get; set; }
        public string LinkTarget { get; } = string.Empty;

        public Stream OpenStream()
        {
            return _streamFactory();
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