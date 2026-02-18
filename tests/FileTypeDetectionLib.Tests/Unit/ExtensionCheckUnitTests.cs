using System.IO.Compression;
using System.Text;
using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ExtensionCheckUnitTests
{
    [Fact]
    public void DetectAndVerifyExtension_ReturnsTrue_ForPdfPath()
    {
        var path = TestResources.Resolve("sample.pdf");
        var detector = new FileTypeDetector();

        var detected = detector.Detect(path);
        var verified = detector.DetectAndVerifyExtension(path);

        Assert.True(verified, $"detected={detected.Kind}, path={path}");
    }

    [Fact]
    public void Detect_DoesNotTrustFileNameExtension_WhenHeaderIsUnknown()
    {
        var detector = new FileTypeDetector();
        var path = Path.Combine(Path.GetTempPath(), "ftd-fake-" + Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, new byte[] { 0x01, 0x23, 0x45, 0x67 });

        try
        {
            var detected = detector.Detect(path);
            Assert.Equal(FileKind.Unknown, detected.Kind);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DetectAndVerifyExtension_AcceptsArchiveAlias_ForArchiveContent()
    {
        var detector = new FileTypeDetector();
        var source = TestResources.Resolve("sample.zip");
        var path = Path.Combine(Path.GetTempPath(), "ftd-zip-alias-" + Guid.NewGuid().ToString("N") + ".tar");

        File.Copy(source, path);
        try
        {
            var detected = detector.Detect(path);
            var verified = detector.DetectAndVerifyExtension(path);

            Assert.Equal(FileKind.Zip, detected.Kind);
            Assert.True(verified);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Detect_DocxPayloadWithPdfExtension_RemainsDocx_UnlessVerifyExtensionIsTrue()
    {
        var detector = new FileTypeDetector();
        var source = TestResources.Resolve("sample.docx");
        var path = Path.Combine(Path.GetTempPath(), "ftd-docx-disguised-" + Guid.NewGuid().ToString("N") + ".pdf");

        File.Copy(source, path);
        try
        {
            var detectedWithoutExtensionPolicy = detector.Detect(path);
            var detectedWithExtensionPolicy = detector.Detect(path, true);

            Assert.Equal(FileKind.Docx, detectedWithoutExtensionPolicy.Kind);
            Assert.Equal(FileKind.Unknown, detectedWithExtensionPolicy.Kind);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DetectAndVerifyExtension_AcceptsXlsmExtension_ForSpreadsheetOpenXmlPayload()
    {
        var detector = new FileTypeDetector();
        var path = Path.Combine(Path.GetTempPath(), "ftd-xlsm-" + Guid.NewGuid().ToString("N") + ".xlsm");
        File.WriteAllBytes(path, CreateOpenXmlPackage("xl/workbook.xml"));

        try
        {
            var detected = detector.Detect(path, true);
            Assert.Equal(FileKind.Xlsx, detected.Kind);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DetectAndVerifyExtension_AcceptsXlsbExtension_ForSpreadsheetBinaryWorkbookPayload()
    {
        var detector = new FileTypeDetector();
        var path = Path.Combine(Path.GetTempPath(), "ftd-xlsb-" + Guid.NewGuid().ToString("N") + ".xlsb");
        File.WriteAllBytes(path, CreateOpenXmlPackage("xl/workbook.bin"));

        try
        {
            var detected = detector.Detect(path, true);
            Assert.Equal(FileKind.Xlsx, detected.Kind);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DetectAndVerifyExtension_AcceptsOdsExtension_ForOpenDocumentSpreadsheetPayload()
    {
        var detector = new FileTypeDetector();
        var path = Path.Combine(Path.GetTempPath(), "ftd-ods-" + Guid.NewGuid().ToString("N") + ".ods");
        File.WriteAllBytes(path, CreateOpenDocumentPackage("application/vnd.oasis.opendocument.spreadsheet"));

        try
        {
            var detected = detector.Detect(path, true);
            Assert.Equal(FileKind.Xlsx, detected.Kind);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DetectAndVerifyExtension_AcceptsDocExtension_ForLegacyOfficePayload()
    {
        var detector = new FileTypeDetector();
        var path = Path.Combine(Path.GetTempPath(), "ftd-doc-" + Guid.NewGuid().ToString("N") + ".doc");
        File.WriteAllBytes(path, CreateOleLikePayload("WordDocument"));

        try
        {
            var detected = detector.Detect(path, true);
            Assert.Equal(FileKind.Docx, detected.Kind);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static byte[] CreateOpenXmlPackage(string markerPath)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            zip.CreateEntry("[Content_Types].xml");
            zip.CreateEntry(markerPath);
        }

        return ms.ToArray();
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
