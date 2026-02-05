using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeSecurityBaselineUnitTests
{
    [Fact]
    public void ApplyDeterministicDefaults_SetsExpectedLimits()
    {
        using var scope = new DetectorOptionsScope();
        FileTypeSecurityBaseline.ApplyDeterministicDefaults();
        var options = FileTypeDetector.GetDefaultOptions();

        Assert.Equal(128L * 1024L * 1024L, options.MaxBytes);
        Assert.Equal(3000, options.MaxZipEntries);
        Assert.Equal(30, options.MaxZipCompressionRatio);
        Assert.True(options.RejectArchiveLinks);
        Assert.False(options.AllowUnknownArchiveEntrySize);
    }
}
