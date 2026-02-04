using System;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ZipAdversarialTests
{
    [Fact]
    public void Detect_FailsClosed_ForManySmallEntries()
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipEntries = 50;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var adversarialZip = ZipPayloadFactory.CreateZipWithEntries(entryCount: 200, entrySize: 1);
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

        var deepZip = ZipPayloadFactory.CreateDeepNestedZip(depth: 3, innerPayloadSize: 16);
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

        var zip = ZipPayloadFactory.CreateZipWithEntrySizes(1024, 8, 8);
        var result = new FileTypeDetector().Detect(zip);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void Detect_FailsClosed_WhenNestedZipDepthExceeded_ForNonZipEntryName()
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipNestingDepth = 1;
        options.MaxZipCompressionRatio = 0;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var deepZip = ZipPayloadFactory.CreateDeepNestedZipWithEntryName(depth: 3, innerPayloadSize: 16, entryName: "payload.bin");
        var result = new FileTypeDetector().Detect(deepZip);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }
}
