using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveInternalsEarlyReturnUnitTests
{
    [Fact]
    public void TryExtractArchiveStreamToMemory_ReturnsEmpty_ForInvalidInputs()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        using var ms = new MemoryStream();

        var descriptor = ArchiveDescriptor.UnknownDescriptor();
        var empty = ArchiveExtractor.TryExtractArchiveStreamToMemory(ms, opt, descriptor);
        Assert.Empty(empty);
    }

    [Fact]
    public void TryExtractArchiveStream_ReturnsFalse_ForInvalidDestination()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        using var ms = new MemoryStream(ArchiveEntryPayloadFactory.CreateZipWithEntries(1, 4));

        Assert.False(ArchiveExtractor.TryExtractArchiveStream(ms, string.Empty, opt));
    }

    [Fact]
    public void ArchiveTypeResolver_TryDescribeBytes_ReturnsFalse_ForNonArchive()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        Assert.False(ArchiveTypeResolver.TryDescribeBytes(new byte[] { 0x00, 0x01 }, opt, ref descriptor));
    }

    [Fact]
    public void ArchiveProcessingEngine_FailsForUnknownDescriptor()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();

        using var ms = new MemoryStream();

        var descriptor = ArchiveDescriptor.UnknownDescriptor();
        var empty = ArchiveExtractor.TryExtractArchiveStreamToMemory(ms, opt, descriptor);
        Assert.Empty(empty);
        Assert.False(ArchiveProcessingEngine.ProcessArchiveStream(ms, opt, depth: 0, descriptor, extractEntry: null));
    }

    [Fact]
    public void EnsureTrailingSeparator_AppendsSeparator_WhenMissing()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("EnsureTrailingSeparator", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var value = TestGuard.NotNull(method.Invoke(null, new object[] { "a/b" }) as string);
        Assert.EndsWith(Path.DirectorySeparatorChar.ToString(), value);
    }

    [Fact]
    public void EnsureTrailingSeparator_ReturnsSeparator_ForEmpty()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("EnsureTrailingSeparator", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var value = TestGuard.NotNull(method.Invoke(null, new object[] { string.Empty }) as string);
        Assert.Equal(Path.DirectorySeparatorChar.ToString(), value);
    }

    [Fact]
    public void EnsureTrailingSeparator_PreservesExistingSeparator()
    {
        var method =
            typeof(ArchiveExtractor).GetMethod("EnsureTrailingSeparator", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var suffix = Path.DirectorySeparatorChar.ToString();
        var value = TestGuard.NotNull(method.Invoke(null, new object[] { "a" + suffix }) as string);
        Assert.Equal("a" + suffix, value);
    }
}
