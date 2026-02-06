using System;
using System.Reflection;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveInternalsReflectionUnitTests
{
    private sealed class FakeEntry : IArchiveEntryModel
    {
        public string RelativePath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long? UncompressedSize { get; set; }
        public long? CompressedSize { get; set; }
        public string LinkTarget { get; set; } = string.Empty;
        public System.IO.Stream OpenStream() => System.IO.Stream.Null;
    }

    [Fact]
    public void TryGetSafeEntryName_RejectsLinkTarget_WhenConfigured()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.RejectArchiveLinks = true;

        var entry = new FakeEntry
        {
            RelativePath = "a.txt",
            LinkTarget = "b.txt"
        };

        var method = typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object[] args = { entry, opt, string.Empty, false };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.False(ok);
    }

    [Fact]
    public void TryGetSafeEntryName_NormalizesDirectoryMarker()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var entry = new FakeEntry { RelativePath = "dir/" };

        var method = typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object[] args = { entry, opt, string.Empty, false };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.True(ok);
        Assert.Equal("dir/", args[2]);
        Assert.True((bool)args[3]);
    }

    [Fact]
    public void ValidateEntrySize_RespectsLimits_AndUnknownSizePolicy()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntryUncompressedBytes = 5;
        opt.AllowUnknownArchiveEntrySize = false;

        var method = typeof(ArchiveExtractor).GetMethod("ValidateEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var okSmall = (bool)method!.Invoke(null, new object[] { new FakeEntry { UncompressedSize = 4 }, opt })!;
        var okLarge = (bool)method.Invoke(null, new object[] { new FakeEntry { UncompressedSize = 6 }, opt })!;
        var okUnknown = (bool)method.Invoke(null, new object[] { new FakeEntry { UncompressedSize = null }, opt })!;

        Assert.True(okSmall);
        Assert.False(okLarge);
        Assert.False(okUnknown);

        opt.AllowUnknownArchiveEntrySize = true;
        var okUnknownAllowed = (bool)method.Invoke(null, new object[] { new FakeEntry { UncompressedSize = null }, opt })!;
        Assert.True(okUnknownAllowed);
    }

    [Fact]
    public void TryGetSafeEntryName_ReturnsFalse_ForInvalidPath()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var entry = new FakeEntry { RelativePath = "../evil.txt" };

        var method = typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object[] args = { entry, opt, string.Empty, false };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.False(ok);
    }

    [Fact]
    public void TryGetSafeEntryName_ReturnsFalse_ForNullEntryOrOptions()
    {
        var method = typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();

        object[] argsEntryNull = { null!, opt, string.Empty, false };
        object[] argsOptNull = { new FakeEntry { RelativePath = "a.txt" }, null!, string.Empty, false };

        Assert.False((bool)method!.Invoke(null, argsEntryNull)!);
        Assert.False((bool)method.Invoke(null, argsOptNull)!);
    }

    [Fact]
    public void TryGetSafeEntryName_AllowsLink_WhenNotRejected()
    {
        var method = typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.RejectArchiveLinks = false;
        var entry = new FakeEntry { RelativePath = "a.txt", LinkTarget = "b.txt" };

        object[] args = { entry, opt, string.Empty, false };
        var ok = (bool)method!.Invoke(null, args)!;

        Assert.True(ok);
        Assert.Equal("a.txt", args[2]);
    }

    [Fact]
    public void ValidateEntrySize_ReturnsFalse_ForNullInputs()
    {
        var method = typeof(ArchiveExtractor).GetMethod("ValidateEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        Assert.False((bool)method!.Invoke(null, new object?[] { null, opt })!);
        Assert.False((bool)method.Invoke(null, new object?[] { new FakeEntry(), null })!);
    }

    [Fact]
    public void ValidateEntrySize_ReturnsTrue_ForDirectoryEntry()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var method = typeof(ArchiveExtractor).GetMethod("ValidateEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var ok = (bool)method!.Invoke(null, new object[] { new FakeEntry { IsDirectory = true }, opt })!;

        Assert.True(ok);
    }

    [Fact]
    public void ValidateEntrySize_RespectsUnknownSize_ForNegativeValues()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var method = typeof(ArchiveExtractor).GetMethod("ValidateEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        opt.AllowUnknownArchiveEntrySize = false;
        var deny = (bool)method!.Invoke(null, new object[] { new FakeEntry { UncompressedSize = -1 }, opt })!;
        Assert.False(deny);

        opt.AllowUnknownArchiveEntrySize = true;
        var allow = (bool)method.Invoke(null, new object[] { new FakeEntry { UncompressedSize = -1 }, opt })!;
        Assert.True(allow);
    }
}
