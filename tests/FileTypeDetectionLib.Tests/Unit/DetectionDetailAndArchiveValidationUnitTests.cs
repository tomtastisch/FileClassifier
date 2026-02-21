using System.IO.Compression;
using System.Text;
using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DetectionDetailAndArchiveValidationUnitTests
{
    [Fact]
    public void DetectDetailed_ReturnsStructuredArchiveTrace_ForDocx()
    {
        var path = TestResources.Resolve("sample.docx");
        var detail = new FileTypeDetector().DetectDetailed(path);

        Assert.Equal(FileKind.Docx, detail.DetectedType.Kind);
        Assert.Equal("ArchiveStructuredRefined", detail.ReasonCode);
        Assert.True(detail.UsedZipContentCheck);
        Assert.True(detail.UsedStructuredRefinement);
        Assert.False(detail.ExtensionVerified);
    }

    [Fact]
    public void DetectDetailed_WithVerifyExtension_FailsClosed_OnExtensionMismatch()
    {
        var path = TestResources.Resolve("sample.changed.pdf.txt");
        var detail = new FileTypeDetector().DetectDetailed(path, true);

        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
        Assert.Equal("ExtensionMismatch", detail.ReasonCode);
        Assert.False(detail.ExtensionVerified);
    }

    [Fact]
    public void TryValidateArchive_ReturnsExpectedResult_ForKnownInputs()
    {
        Assert.True(FileTypeDetector.TryValidateArchive(TestResources.Resolve("sample.zip")));
        Assert.False(FileTypeDetector.TryValidateArchive(TestResources.Resolve("sample.docx")));
        Assert.False(FileTypeDetector.TryValidateArchive(TestResources.Resolve("sample.pdf")));
    }

    [Fact]
    public void TryValidateArchive_ReturnsFalse_ForMissingFile()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "ftd-missing-" + Guid.NewGuid().ToString("N") + ".zip");
        Assert.False(FileTypeDetector.TryValidateArchive(missingPath));
    }

    [Fact]
    public void TryValidateArchive_ReturnsFalse_ForOpenDocumentSpreadsheet()
    {
        var path = Path.Combine(Path.GetTempPath(), "ftd-ods-" + Guid.NewGuid().ToString("N") + ".ods");
        File.WriteAllBytes(path, CreateOpenDocumentPackage("application/vnd.oasis.opendocument.spreadsheet"));

        try
        {
            Assert.False(FileTypeDetector.TryValidateArchive(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void TryValidateArchive_ReturnsFalse_ForLegacyWordDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), "ftd-doc-" + Guid.NewGuid().ToString("N") + ".doc");
        File.WriteAllBytes(path, CreateOleLikePayload("WordDocument"));

        try
        {
            Assert.False(FileTypeDetector.TryValidateArchive(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static byte[] CreateOpenDocumentPackage(string mimeType)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var mimeEntry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(mimeEntry.Open(), Encoding.ASCII, 1024, leaveOpen: false))
            {
                writer.Write(mimeType);
            }

            zip.CreateEntry("content.xml");
        }

        return ms.ToArray();
    }

    private static byte[] CreateOleLikePayload(string marker)
    {
        var payload = new byte[1024];
        var oleSignature = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        Buffer.BlockCopy(oleSignature, 0, payload, 0, oleSignature.Length);

        var markerBytes = Encoding.ASCII.GetBytes(marker);
        Buffer.BlockCopy(markerBytes, 0, payload, 256, markerBytes.Length);
        return payload;
    }
}
