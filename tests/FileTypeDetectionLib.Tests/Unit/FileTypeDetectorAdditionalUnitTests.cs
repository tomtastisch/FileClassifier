using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeDetectorAdditionalUnitTests
{
    [Fact]
    public void LoadOptions_ReturnsDefaults_ForMissingOrNonJson()
    {
        var missing = FileTypeDetector.LoadOptions("missing.json");
        var notJson = FileTypeDetector.LoadOptions("config.txt");

        Assert.NotNull(missing);
        Assert.NotNull(notJson);
        Assert.Equal(FileTypeProjectOptions.DefaultOptions().MaxBytes, missing.MaxBytes);
        Assert.Equal(FileTypeProjectOptions.DefaultOptions().MaxBytes, notJson.MaxBytes);
    }

    [Fact]
    public void LoadOptions_ReturnsDefaults_ForInvalidJson()
    {
        using var scope = TestTempPaths.CreateScope("ftd-options");
        var path = Path.Combine(scope.RootPath, "bad.json");
        File.WriteAllText(path, "{ invalid json");

        var options = FileTypeDetector.LoadOptions(path);

        Assert.NotNull(options);
        Assert.Equal(FileTypeProjectOptions.DefaultOptions().MaxBytes, options.MaxBytes);
    }

    [Fact]
    public void ReadFileSafe_ReturnsEmpty_ForMissingFile()
    {
        var data = FileTypeDetector.ReadFileSafe("missing.bin");
        Assert.Empty(data);
    }

    [Fact]
    public void ReadFileSafe_ReturnsEmpty_WhenFileTooLarge()
    {
        var original = FileTypeDetector.GetDefaultOptions();
        var custom = FileTypeProjectOptions.DefaultOptions();
        custom.MaxBytes = 1;
        FileTypeDetector.SetDefaultOptions(custom);

        using var scope = TestTempPaths.CreateScope("ftd-readsafe");
        var path = Path.Combine(scope.RootPath, "big.bin");
        File.WriteAllBytes(path, new byte[] { 0x01, 0x02, 0x03 });

        try
        {
            var data = FileTypeDetector.ReadFileSafe(path);
            Assert.Empty(data);
        }
        finally
        {
            FileTypeDetector.SetDefaultOptions(original);
        }
    }

    [Fact]
    public void ReadFileSafe_ReadsWithinLimit()
    {
        using var scope = TestTempPaths.CreateScope("ftd-readsafe-ok");
        var path = Path.Combine(scope.RootPath, "small.bin");
        var payload = new byte[] { 0x01, 0x02 };
        File.WriteAllBytes(path, payload);

        var data = FileTypeDetector.ReadFileSafe(path);

        Assert.Equal(payload, data);
    }

    [Fact]
    public void Detect_ReturnsUnknown_WhenFileTooLarge_ForConfiguredMaxBytes()
    {
        var original = FileTypeOptions.GetSnapshot();
        var custom = FileTypeOptions.GetSnapshot();
        custom.MaxBytes = 1;
        FileTypeOptions.SetSnapshot(custom);

        using var scope = TestTempPaths.CreateScope("ftd-detector-maxbytes");
        var path = Path.Combine(scope.RootPath, "payload.bin");
        File.WriteAllBytes(path, new byte[] { 0x01, 0x02, 0x03 });

        try
        {
            var detected = new FileTypeDetector().Detect(path);
            Assert.Equal(FileKind.Unknown, detected.Kind);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void Detect_ReturnsUnknown_WhenBytePayloadTooLarge()
    {
        var original = FileTypeOptions.GetSnapshot();
        var custom = FileTypeOptions.GetSnapshot();
        custom.MaxBytes = 1;
        FileTypeOptions.SetSnapshot(custom);

        try
        {
            var detected = new FileTypeDetector().Detect(new byte[] { 0x01, 0x02 });
            Assert.Equal(FileKind.Unknown, detected.Kind);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void Detect_ReturnsPdf_ForPdfBytes()
    {
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        var detected = new FileTypeDetector().Detect(payload);

        Assert.Equal(FileKind.Pdf, detected.Kind);
    }

    [Fact]
    public void Detect_ReturnsUnknown_ForInvalidZipMagicBytes()
    {
        var payload = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00 };

        var detected = new FileTypeDetector().Detect(payload);

        Assert.Equal(FileKind.Unknown, detected.Kind);
    }

    [Fact]
    public void Detect_ReturnsUnknown_ForNullBytes()
    {
        var detected = new FileTypeDetector().Detect((byte[])null!);

        Assert.Equal(FileKind.Unknown, detected.Kind);
    }

    [Fact]
    public void Detect_ReturnsZip_ForTarBytes()
    {
        var payload = ArchivePayloadFactory.CreateTarWithSingleEntry("note.txt", "hello");

        var detected = new FileTypeDetector().Detect(payload);

        Assert.Equal(FileKind.Zip, detected.Kind);
    }
}