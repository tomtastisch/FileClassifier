using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeProjectBaselineUnitTests
{
    [Fact]
    public void ApplyDeterministicDefaults_SetsExpectedLimits()
    {
        using var scope = new DetectorOptionsScope();
        FileTypeProjectBaseline.ApplyDeterministicDefaults();
        var options = FileTypeDetector.GetDefaultOptions();

        Assert.Equal(128L * 1024L * 1024L, options.MaxBytes);
        Assert.Equal(3000, options.MaxZipEntries);
        Assert.Equal(30, options.MaxZipCompressionRatio);
        Assert.True(options.RejectArchiveLinks);
        Assert.False(options.AllowUnknownArchiveEntrySize);
        Assert.False(options.DeterministicHash.IncludePayloadCopies);
        Assert.True(options.DeterministicHash.IncludeFastHash);
        Assert.Equal("deterministic-roundtrip.bin", options.DeterministicHash.MaterializedFileName);
    }
}
