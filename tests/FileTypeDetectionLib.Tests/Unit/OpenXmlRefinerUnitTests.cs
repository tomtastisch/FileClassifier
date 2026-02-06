using System.IO.Compression;
using FileTypeDetection;
using Xunit;

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
    [InlineData("word/document.xml", FileKind.Docx)]
    [InlineData("xl/workbook.xml", FileKind.Xlsx)]
    [InlineData("ppt/presentation.xml", FileKind.Pptx)]
    public void TryRefineStream_DetectsOpenXmlKinds(string markerPath, FileKind expected)
    {
        var payload = CreateOpenXmlPackage(markerPath);
        using var stream = new MemoryStream(payload, false);

        var result = OpenXmlRefiner.TryRefineStream(stream);

        Assert.Equal(expected, result.Kind);
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
}