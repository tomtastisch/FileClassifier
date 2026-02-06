using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveInternalsReflectionUnitTests
{
    [Fact]
    public void TryGetSafeEntryName_RejectsLinkTarget_WhenConfigured()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.RejectArchiveLinks = true;

        var entry = new FakeEntry(relativePath: "a.txt", linkTarget: "b.txt");

        var method =
            typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object[] args = { entry, opt, string.Empty, false };
        var ok = TestGuard.Unbox<bool>(method!.Invoke(null, args));

        Assert.False(ok);
    }

    [Fact]
    public void TryGetSafeEntryName_NormalizesDirectoryMarker()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var entry = new FakeEntry(relativePath: "dir/");

        var method =
            typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object[] args = { entry, opt, string.Empty, false };
        var ok = TestGuard.Unbox<bool>(method!.Invoke(null, args));

        Assert.True(ok);
        Assert.Equal("dir/", args[2]);
        Assert.True(TestGuard.Unbox<bool>(args[3]));
    }

    [Fact]
    public void ValidateEntrySize_RespectsLimits_AndUnknownSizePolicy()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxZipEntryUncompressedBytes = 5;
        opt.AllowUnknownArchiveEntrySize = false;

        var method =
            typeof(ArchiveExtractor).GetMethod("ValidateEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var okSmall = TestGuard.Unbox<bool>(method!.Invoke(null, new object[] { new FakeEntry(uncompressedSize: 4), opt }));
        var okLarge = TestGuard.Unbox<bool>(method.Invoke(null, new object[] { new FakeEntry(uncompressedSize: 6), opt }));
        var okUnknown = TestGuard.Unbox<bool>(method.Invoke(null, new object[] { new FakeEntry(uncompressedSize: null), opt }));

        Assert.True(okSmall);
        Assert.False(okLarge);
        Assert.False(okUnknown);

        opt.AllowUnknownArchiveEntrySize = true;
        var okUnknownAllowed =
            TestGuard.Unbox<bool>(method.Invoke(null, new object[] { new FakeEntry(uncompressedSize: null), opt }));
        Assert.True(okUnknownAllowed);
    }

    [Fact]
    public void TryGetSafeEntryName_ReturnsFalse_ForInvalidPath()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var entry = new FakeEntry(relativePath: "../evil.txt");

        var method =
            typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object[] args = { entry, opt, string.Empty, false };
        var ok = TestGuard.Unbox<bool>(method!.Invoke(null, args));

        Assert.False(ok);
    }

    [Fact]
    public void TryGetSafeEntryName_ReturnsFalse_ForNullEntryOrOptions()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();

        object[] argsEntryNull = { null!, opt, string.Empty, false };
        object[] argsOptNull = { new FakeEntry(relativePath: "a.txt"), null!, string.Empty, false };

        Assert.False(TestGuard.Unbox<bool>(method!.Invoke(null, argsEntryNull)));
        Assert.False(TestGuard.Unbox<bool>(method.Invoke(null, argsOptNull)));
    }

    [Fact]
    public void TryGetSafeEntryName_AllowsLink_WhenNotRejected()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("TryGetSafeEntryName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.RejectArchiveLinks = false;
        var entry = new FakeEntry(relativePath: "a.txt", linkTarget: "b.txt");

        object[] args = { entry, opt, string.Empty, false };
        var ok = TestGuard.Unbox<bool>(method!.Invoke(null, args));

        Assert.True(ok);
        Assert.Equal("a.txt", args[2]);
    }

    [Fact]
    public void ValidateEntrySize_ReturnsFalse_ForNullInputs()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("ValidateEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var opt = FileTypeProjectOptions.DefaultOptions();
        Assert.False(TestGuard.Unbox<bool>(method!.Invoke(null, new object?[] { null, opt })));
        Assert.False(TestGuard.Unbox<bool>(method.Invoke(null, new object?[] { new FakeEntry(), null })));
    }

    [Fact]
    public void ValidateEntrySize_ReturnsTrue_ForDirectoryEntry()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var method =
            typeof(ArchiveExtractor).GetMethod("ValidateEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var ok = TestGuard.Unbox<bool>(method!.Invoke(null, new object[] { new FakeEntry(isDirectory: true), opt }));

        Assert.True(ok);
    }

    [Fact]
    public void ValidateEntrySize_RespectsUnknownSize_ForNegativeValues()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var method =
            typeof(ArchiveExtractor).GetMethod("ValidateEntrySize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        opt.AllowUnknownArchiveEntrySize = false;
        var deny = TestGuard.Unbox<bool>(method!.Invoke(null, new object[] { new FakeEntry(uncompressedSize: -1), opt }));
        Assert.False(deny);

        opt.AllowUnknownArchiveEntrySize = true;
        var allow = TestGuard.Unbox<bool>(method.Invoke(null, new object[] { new FakeEntry(uncompressedSize: -1), opt }));
        Assert.True(allow);
    }

    private sealed class FakeEntry : IArchiveEntryModel
    {
        public FakeEntry(string? relativePath = null, long? uncompressedSize = null, long? compressedSize = null,
            bool isDirectory = false, string? linkTarget = null)
        {
            RelativePath = relativePath ?? string.Empty;
            UncompressedSize = uncompressedSize;
            CompressedSize = compressedSize;
            IsDirectory = isDirectory;
            LinkTarget = linkTarget ?? string.Empty;
        }

        public string RelativePath { get; }
        public bool IsDirectory { get; }
        public long? UncompressedSize { get; }
        public long? CompressedSize { get; }
        public string LinkTarget { get; }

        public Stream OpenStream()
        {
            return Stream.Null;
        }
    }
}
