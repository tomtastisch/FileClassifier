using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveAdversarialTests
{
    [Fact]
    public void Detect_FailsClosed_ForManySmallEntries()
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipEntries = 50;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var adversarialZip = ArchiveEntryPayloadFactory.CreateZipWithEntries(200, 1);
        var result = new FileTypeDetector().Detect(adversarialZip);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void Detect_FailsClosed_WhenNestedDepthExceeded()
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipNestingDepth = 1;
        options.MaxZipCompressionRatio = 0;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var deepZip = ArchiveEntryPayloadFactory.CreateDeepNestedZip(3, 16);
        var result = new FileTypeDetector().Detect(deepZip);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void Detect_FailsClosed_OnFirstInvalidEntry()
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipEntryUncompressedBytes = 128;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var zip = ArchiveEntryPayloadFactory.CreateZipWithEntrySizes(1024, 8, 8);
        var result = new FileTypeDetector().Detect(zip);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void Detect_FailsClosed_WhenNestedArchiveDepthExceeded_ForNonArchiveEntryName()
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipNestingDepth = 1;
        options.MaxZipCompressionRatio = 0;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var deepZip = ArchiveEntryPayloadFactory.CreateDeepNestedZipWithEntryName(3, 16, "payload.bin");
        var result = new FileTypeDetector().Detect(deepZip);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }
}