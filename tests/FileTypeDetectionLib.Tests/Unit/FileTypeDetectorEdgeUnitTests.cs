using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeDetectorEdgeUnitTests
{
    [Fact]
    public void DetectDetailed_ReturnsFileNotFound_ForMissingPath()
    {
        var detector = new FileTypeDetector();
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");

        var detail = detector.DetectDetailed(missing);

        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
        Assert.Equal("FileNotFound", detail.ReasonCode);
    }

    [Fact]
    public void DetectDetailed_ReturnsExtensionMismatch_WhenVerifyExtensionFails()
    {
        using var scope = TestTempPaths.CreateScope("ftd-ext-mismatch");
        var wrongPath = Path.Combine(scope.RootPath, "sample.txt");
        File.Copy(TestResources.Resolve("sample.pdf"), wrongPath);

        var detail = new FileTypeDetector().DetectDetailed(wrongPath, verifyExtension: true);

        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
        Assert.Equal("ExtensionMismatch", detail.ReasonCode);
        Assert.False(detail.ExtensionVerified);
    }

    [Fact]
    public void DetectDetailed_ReturnsArchiveGateFailed_ForUnsafeArchive()
    {
        using var scope = TestTempPaths.CreateScope("ftd-unsafe-archive");
        var zipPath = Path.Combine(scope.RootPath, "unsafe.zip");
        File.WriteAllBytes(zipPath, ArchiveEntryPayloadFactory.CreateZipWithEntries(2, 4));

        using var optionsScope = new DetectorOptionsScope();
        var opt = FileTypeDetector.GetDefaultOptions();
        opt.MaxZipEntries = 1;
        optionsScope.Set(opt);

        var detail = new FileTypeDetector().DetectDetailed(zipPath);

        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
        Assert.Equal("ArchiveGateFailed", detail.ReasonCode);
        Assert.True(detail.UsedZipContentCheck);
    }

    [Fact]
    public void DetectDetailed_ReturnsStructuredRefined_ForDocx()
    {
        var path = TestResources.Resolve("sample.docx");
        var detail = new FileTypeDetector().DetectDetailed(path);

        Assert.Equal(FileKind.Docx, detail.DetectedType.Kind);
        Assert.Equal("ArchiveStructuredRefined", detail.ReasonCode);
        Assert.True(detail.UsedStructuredRefinement);
    }

    [Fact]
    public void DetectDetailed_ReturnsArchiveGeneric_ForTar()
    {
        using var scope = TestTempPaths.CreateScope("ftd-tar");
        var tarPath = Path.Combine(scope.RootPath, "sample.tar");
        File.WriteAllBytes(tarPath, ArchivePayloadFactory.CreateTarWithSingleEntry("note.txt", "hello"));

        var detail = new FileTypeDetector().DetectDetailed(tarPath);

        Assert.Equal(FileKind.Zip, detail.DetectedType.Kind);
        Assert.Equal("ArchiveGeneric", detail.ReasonCode);
    }

    [Fact]
    public void Detect_ReturnsUnknown_WhenPayloadExceedsMaxBytes()
    {
        using var optionsScope = new DetectorOptionsScope();
        var opt = FileTypeDetector.GetDefaultOptions();
        opt.MaxBytes = 4;
        optionsScope.Set(opt);

        var detected = new FileTypeDetector().Detect(new byte[10]);

        Assert.Equal(FileKind.Unknown, detected.Kind);
    }

    [Fact]
    public void ReadFileSafe_ReturnsEmpty_WhenFileTooLarge()
    {
        using var optionsScope = new DetectorOptionsScope();
        var opt = FileTypeDetector.GetDefaultOptions();
        opt.MaxBytes = 1;
        optionsScope.Set(opt);

        using var temp = TestTempPaths.CreateScope("ftd-readfile");
        var path = Path.Combine(temp.RootPath, "big.bin");
        File.WriteAllBytes(path, new byte[10]);

        var data = FileTypeDetector.ReadFileSafe(path);

        Assert.Empty(data);
    }
}
