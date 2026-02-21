using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class EndToEndFailClosedMatrixUnitTests
{
    public static TheoryData<string, FileKind, bool> SupportedFixtureMatrix => new()
    {
        { "sample.pdf", FileKind.Pdf, false },
        { "sample.png", FileKind.Png, false },
        { "sample.jpg", FileKind.Jpeg, false },
        { "sample.gif", FileKind.Gif, false },
        { "sample.webp", FileKind.Webp, false },
        { "sample.docx", FileKind.Doc, false },
        { "sample.xlsx", FileKind.Xls, false },
        { "sample.pptx", FileKind.Ppt, false },
        { "sample.zip", FileKind.Zip, true },
        { "sample.7z", FileKind.Zip, true },
        { "sample.rar", FileKind.Zip, true },
        { "sample_pdf_no_extension", FileKind.Pdf, false }
    };

    public static TheoryData<string, string, FileKind> SupportedAliasMatrix => new()
    {
        { "sample.docx", ".doc", FileKind.Doc },
        { "sample.docx", ".docm", FileKind.Doc },
        { "sample.docx", ".docb", FileKind.Doc },
        { "sample.docx", ".dot", FileKind.Doc },
        { "sample.docx", ".dotm", FileKind.Doc },
        { "sample.docx", ".dotx", FileKind.Doc },
        { "sample.docx", ".odt", FileKind.Doc },
        { "sample.docx", ".ott", FileKind.Doc },
        { "sample.xlsx", ".xls", FileKind.Xls },
        { "sample.xlsx", ".xlsm", FileKind.Xls },
        { "sample.xlsx", ".xlsb", FileKind.Xls },
        { "sample.xlsx", ".xlt", FileKind.Xls },
        { "sample.xlsx", ".xltm", FileKind.Xls },
        { "sample.xlsx", ".xltx", FileKind.Xls },
        { "sample.xlsx", ".xltb", FileKind.Xls },
        { "sample.xlsx", ".xlam", FileKind.Xls },
        { "sample.xlsx", ".xla", FileKind.Xls },
        { "sample.xlsx", ".ods", FileKind.Xls },
        { "sample.xlsx", ".ots", FileKind.Xls },
        { "sample.pptx", ".ppt", FileKind.Ppt },
        { "sample.pptx", ".pptm", FileKind.Ppt },
        { "sample.pptx", ".pot", FileKind.Ppt },
        { "sample.pptx", ".potm", FileKind.Ppt },
        { "sample.pptx", ".potx", FileKind.Ppt },
        { "sample.pptx", ".pps", FileKind.Ppt },
        { "sample.pptx", ".ppsm", FileKind.Ppt },
        { "sample.pptx", ".ppsx", FileKind.Ppt },
        { "sample.pptx", ".odp", FileKind.Ppt },
        { "sample.pptx", ".otp", FileKind.Ppt },
        { "sample.zip", ".tar", FileKind.Zip },
        { "sample.zip", ".tgz", FileKind.Zip },
        { "sample.zip", ".tar.gz", FileKind.Zip },
        { "sample.zip", ".gz", FileKind.Zip },
        { "sample.zip", ".bz2", FileKind.Zip },
        { "sample.zip", ".xz", FileKind.Zip },
        { "sample.zip", ".7z", FileKind.Zip },
        { "sample.zip", ".rar", FileKind.Zip }
    };

    public static TheoryData<string> CorruptExtensionMatrix => new()
    {
        ".pdf",
        ".png",
        ".jpg",
        ".gif",
        ".webp",
        ".docx",
        ".xlsx",
        ".pptx",
        ".zip",
        ".7z",
        ".rar",
        ".doc",
        ".xlsm",
        ".xlsb",
        ".odt",
        ".ods",
        ".odp"
    };

    [Theory]
    [MemberData(nameof(SupportedFixtureMatrix))]
    public void Detect_PathBytesAndDetail_AreConsistent_ForSupportedFixtures(string fixture, FileKind expectedKind,
        bool isArchive)
    {
        var detector = new FileTypeDetector();
        var path = TestResources.Resolve(fixture);
        var bytes = File.ReadAllBytes(path);

        var fromPath = detector.Detect(path);
        var fromBytes = detector.Detect(bytes);
        var detail = detector.DetectDetailed(path, false);

        Assert.Equal(expectedKind, fromPath.Kind);
        Assert.Equal(expectedKind, fromBytes.Kind);
        Assert.Equal(expectedKind, detail.DetectedType.Kind);
        Assert.Equal(isArchive, expectedKind == FileKind.Zip);
    }

    [Theory]
    [MemberData(nameof(SupportedFixtureMatrix))]
    public void Detect_WrongExtension_OnlyFailsWithExplicitExtensionVerification(string fixture, FileKind expectedKind,
        bool isArchive)
    {
        using var scope = TestTempPaths.CreateScope("ftd-e2e-mismatch");
        var sourcePath = TestResources.Resolve(fixture);
        var disguisedPath = Path.Combine(scope.RootPath, $"payload-{Guid.NewGuid():N}.bin");
        File.Copy(sourcePath, disguisedPath);

        var detector = new FileTypeDetector();
        var byContent = detector.Detect(disguisedPath);
        var strict = detector.Detect(disguisedPath, true);
        var strictDetail = detector.DetectDetailed(disguisedPath, true);

        Assert.Equal(expectedKind, byContent.Kind);
        Assert.Equal(FileKind.Unknown, strict.Kind);
        Assert.Equal(FileKind.Unknown, strictDetail.DetectedType.Kind);
        Assert.Equal("ExtensionMismatch", strictDetail.ReasonCode);
        Assert.False(strictDetail.ExtensionVerified);
        Assert.Equal(isArchive, byContent.Kind == FileKind.Zip);
    }

    [Theory]
    [MemberData(nameof(SupportedAliasMatrix))]
    public void Detect_RecognizesSupportedAliasExtensions_InStrictMode(string sourceFixture, string aliasExtension,
        FileKind expectedKind)
    {
        using var scope = TestTempPaths.CreateScope("ftd-e2e-alias");
        var sourcePath = TestResources.Resolve(sourceFixture);
        var aliasPath = Path.Combine(scope.RootPath, $"alias-{Guid.NewGuid():N}{aliasExtension}");
        File.Copy(sourcePath, aliasPath);

        var strict = new FileTypeDetector().Detect(aliasPath, true);

        Assert.Equal(expectedKind, strict.Kind);
    }

    [Theory]
    [MemberData(nameof(SupportedFixtureMatrix))]
    public void ArchiveApis_AreFailClosed_AndOnlyAcceptRealArchives(string fixture, FileKind expectedKind, bool isArchive)
    {
        var detector = new FileTypeDetector();
        var path = TestResources.Resolve(fixture);

        var validate = FileTypeDetector.TryValidateArchive(path);
        var entriesStrict = detector.ExtractArchiveSafeToMemory(path, true);
        var entriesRelaxed = detector.ExtractArchiveSafeToMemory(path, false);

        if (isArchive)
        {
            Assert.True(validate);
            Assert.NotEmpty(entriesStrict);
            Assert.NotEmpty(entriesRelaxed);
        }
        else
        {
            Assert.False(validate);
            Assert.Empty(entriesStrict);
            Assert.Empty(entriesRelaxed);
        }

        Assert.Equal(isArchive, expectedKind == FileKind.Zip);
    }

    [Theory]
    [MemberData(nameof(CorruptExtensionMatrix))]
    public void CorruptedPayloads_FailClosed_AcrossDetectionAndArchiveProcessing(string extension)
    {
        using var scope = TestTempPaths.CreateScope("ftd-e2e-corrupt");
        var path = Path.Combine(scope.RootPath, $"corrupt-{Guid.NewGuid():N}{extension}");
        var payload = CreateDeterministicCorruptPayload(extension);
        File.WriteAllBytes(path, payload);

        var detector = new FileTypeDetector();
        var strict = detector.Detect(path, true);
        var detail = detector.DetectDetailed(path, true);
        var validate = FileTypeDetector.TryValidateArchive(path);
        var entriesStrict = detector.ExtractArchiveSafeToMemory(path, true);
        var entriesRelaxed = detector.ExtractArchiveSafeToMemory(path, false);
        var fromBytes = detector.Detect(payload);

        Assert.Equal(FileKind.Unknown, strict.Kind);
        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
        Assert.Equal("ExtensionMismatch", detail.ReasonCode);
        Assert.False(validate);
        Assert.Empty(entriesStrict);
        Assert.Empty(entriesRelaxed);
        Assert.Equal(FileKind.Unknown, fromBytes.Kind);
    }

    private static byte[] CreateDeterministicCorruptPayload(string extension)
    {
        var marker = $"corrupt-payload::{extension}";
        var prefix = System.Text.Encoding.ASCII.GetBytes(marker);
        var payload = new byte[1024];

        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = 0x41;
        }

        Buffer.BlockCopy(prefix, 0, payload, 16, Math.Min(prefix.Length, payload.Length - 16));
        return payload;
    }
}
