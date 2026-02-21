using System.IO.Compression;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class OpenXmlRefinerUnitTests
{
    [Fact]
    public void TryRefine_ReturnsUnknown_ForNullFactory()
    {
        var result = OpenXmlRefiner.TryRefine(null);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void TryRefineStream_ReturnsUnknown_ForUnreadableStream()
    {
        using var stream = new MemoryStream();
        stream.Close();

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Theory]
    [InlineData("word/document.xml", FileKind.Doc)]
    [InlineData("xl/workbook.xml", FileKind.Xls)]
    [InlineData("xl/workbook.bin", FileKind.Xls)]
    [InlineData("ppt/presentation.xml", FileKind.Ppt)]
    public void TryRefineStream_DetectsOpenXmlKinds(string markerPath, FileKind expected)
    {
        var payload = CreateOpenXmlPackage(markerPath);
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(expected, result.Kind);
    }

    [Theory]
    [InlineData("application/vnd.oasis.opendocument.text", FileKind.Doc)]
    [InlineData("application/vnd.oasis.opendocument.text-template", FileKind.Doc)]
    [InlineData("application/vnd.oasis.opendocument.spreadsheet", FileKind.Xls)]
    [InlineData("application/vnd.oasis.opendocument.spreadsheet-template", FileKind.Xls)]
    [InlineData("application/vnd.oasis.opendocument.presentation", FileKind.Ppt)]
    [InlineData("application/vnd.oasis.opendocument.presentation-template", FileKind.Ppt)]
    public void TryRefineStream_DetectsOpenDocumentKinds(string mimeType, FileKind expected)
    {
        var payload = CreateOpenDocumentPackage(mimeType);
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(expected, result.Kind);
    }

    [Fact]
    public void TryRefineStream_ReturnsUnknown_WhenOpenDocumentMimeEntryIsEmpty()
    {
        var payload = CreateOpenDocumentPackage(string.Empty);
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void TryRefineStream_ReturnsUnknown_WhenContentTypesMissing()
    {
        var payload = CreateZipWithEntries("word/document.xml");
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void TryRefineStream_ReturnsUnknown_WhenMarkersMissing()
    {
        var payload = CreateZipWithEntries("[Content_Types].xml");
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void TryRefineStream_ReturnsUnknown_WhenOpenXmlMarkersAreAmbiguous()
    {
        var payload = CreateOpenXmlPackageWithMarkers("word/document.xml", "xl/workbook.xml");
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void TryRefineStream_ReturnsUnknown_WhenOpenXmlAndOpenDocumentSignalsConflict()
    {
        var payload = CreateHybridOfficePackage("application/vnd.oasis.opendocument.text", "word/document.xml");
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void TryRefineStream_ReturnsUnknown_WhenOpenDocumentMimeSignalsConflict()
    {
        var payload = CreateConflictingOpenDocumentMimes(
            "application/vnd.oasis.opendocument.text",
            "application/vnd.oasis.opendocument.spreadsheet");
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(FileKind.Unknown, result.Kind);
    }

    [Fact]
    public void TryRefine_ReturnsUnknown_WhenFactoryThrows()
    {
        var result = OpenXmlRefiner.TryRefine(() => throw new InvalidOperationException("boom"));

        Assert.Equal(FileKind.Unknown, result.Kind);
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

    private static byte[] CreateZipWithEntries(params string[] names)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var name in names) zip.CreateEntry(name);
        }

        return ms.ToArray();
    }

    private static byte[] CreateOpenDocumentPackage(string mimeType)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var mimeEntry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(mimeEntry.Open()))
            {
                writer.Write(mimeType);
            }

            zip.CreateEntry("content.xml");
        }

        return ms.ToArray();
    }

    private static byte[] CreateOpenXmlPackageWithMarkers(params string[] markerPaths)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            zip.CreateEntry("[Content_Types].xml");
            foreach (var markerPath in markerPaths) zip.CreateEntry(markerPath);
        }

        return ms.ToArray();
    }

    private static byte[] CreateHybridOfficePackage(string openDocumentMimeType, string openXmlMarkerPath)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            zip.CreateEntry("[Content_Types].xml");
            zip.CreateEntry(openXmlMarkerPath);

            var mimeEntry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(mimeEntry.Open()))
            {
                writer.Write(openDocumentMimeType);
            }
        }

        return ms.ToArray();
    }

    private static byte[] CreateConflictingOpenDocumentMimes(string firstMime, string secondMime)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var first = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(first.Open()))
            {
                writer.Write(firstMime);
            }

            var second = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(second.Open()))
            {
                writer.Write(secondMime);
            }
        }

        return ms.ToArray();
    }
}
