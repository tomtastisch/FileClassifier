using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Property;

public sealed class ArchiveGatePropertyTests
{
    [Theory]
    [InlineData(2, 2, FileKind.Zip)]
    [InlineData(3, 2, FileKind.Unknown)]
    public void Detect_Respects_MaxArchiveEntries(int entries, int maxEntries, FileKind expected)
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipEntries = maxEntries;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var zip = ArchiveEntryPayloadFactory.CreateZipWithEntries(entries, 8);
        var result = new FileTypeDetector().Detect(zip);

        Assert.Equal(expected, result.Kind);
    }

    [Theory]
    [InlineData(64, 64, FileKind.Zip)]
    [InlineData(65, 64, FileKind.Unknown)]
    public void Detect_Respects_MaxArchiveEntryUncompressedBytes(int entrySize, int maxEntryBytes, FileKind expected)
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipEntryUncompressedBytes = maxEntryBytes;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var zip = ArchiveEntryPayloadFactory.CreateZipWithEntries(1, entrySize);
        var result = new FileTypeDetector().Detect(zip);

        Assert.Equal(expected, result.Kind);
    }

    [Theory]
    [InlineData(2, 40, 80, FileKind.Zip)]
    [InlineData(2, 40, 79, FileKind.Unknown)]
    public void Detect_Respects_MaxArchiveTotalUncompressedBytes(int entries, int entrySize, long maxTotal,
        FileKind expected)
    {
        using var scope = new DetectorOptionsScope();
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxZipTotalUncompressedBytes = maxTotal;
        options.MaxBytes = 10 * 1024 * 1024;
        scope.Set(options);

        var zip = ArchiveEntryPayloadFactory.CreateZipWithEntries(entries, entrySize);
        var result = new FileTypeDetector().Detect(zip);

        Assert.Equal(expected, result.Kind);
    }

    [Fact]
    public void Detect_Respects_MaxArchiveCompressionRatio_WhenEnabled()
    {
        var zip = ArchiveEntryPayloadFactory.CreateZipWithEntries(1, 200_000);

        using (var strictScope = new DetectorOptionsScope())
        {
            var strict = FileTypeDetector.GetDefaultOptions();
            strict.MaxZipCompressionRatio = 1;
            strict.MaxBytes = 10 * 1024 * 1024;
            strictScope.Set(strict);

            var strictResult = new FileTypeDetector().Detect(zip);
            Assert.Equal(FileKind.Unknown, strictResult.Kind);
        }

        using (var relaxedScope = new DetectorOptionsScope())
        {
            var relaxed = FileTypeDetector.GetDefaultOptions();
            relaxed.MaxZipCompressionRatio = 0;
            relaxed.MaxBytes = 10 * 1024 * 1024;
            relaxedScope.Set(relaxed);

            var relaxedResult = new FileTypeDetector().Detect(zip);
            Assert.Equal(FileKind.Zip, relaxedResult.Kind);
        }
    }

    [Fact]
    public void Detect_Respects_MaxArchiveNestedBytes_ForInnerArchive()
    {
        var (zip, innerBytes) = ArchiveEntryPayloadFactory.CreateNestedZipWithInnerLength(2048);

        using (var passScope = new DetectorOptionsScope())
        {
            var passOptions = FileTypeDetector.GetDefaultOptions();
            passOptions.MaxZipNestingDepth = 2;
            passOptions.MaxZipNestedBytes = innerBytes;
            passOptions.MaxZipCompressionRatio = 0;
            passOptions.MaxBytes = 10 * 1024 * 1024;
            passScope.Set(passOptions);

            var passResult = new FileTypeDetector().Detect(zip);
            Assert.Equal(FileKind.Zip, passResult.Kind);
        }

        using (var failScope = new DetectorOptionsScope())
        {
            var failOptions = FileTypeDetector.GetDefaultOptions();
            failOptions.MaxZipNestingDepth = 2;
            failOptions.MaxZipNestedBytes = Math.Max(0, innerBytes - 1);
            failOptions.MaxZipCompressionRatio = 0;
            failOptions.MaxBytes = 10 * 1024 * 1024;
            failScope.Set(failOptions);

            var failResult = new FileTypeDetector().Detect(zip);
            Assert.Equal(FileKind.Unknown, failResult.Kind);
        }
    }
}
