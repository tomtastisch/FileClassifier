using System;
using System.IO;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests;

public class FileTypeDetectorTests
{
    private static string Resource(string name) =>
        Path.Combine(AppContext.BaseDirectory, "resources", name);

    [Fact]
    public void Detect_MinimalPdf_ReturnsPdf()
    {
        var detector = new FileTypeDetector();
        var result = detector.Detect(Resource("sample.pdf"));
        Assert.Equal(FileKind.Pdf, result.Kind);
    }

    [Fact]
    public void Detect_MinimalJpeg_ReturnsJpeg()
    {
        var detector = new FileTypeDetector();
        var result = detector.Detect(Resource("sample.jpg"));
        Assert.Equal(FileKind.Jpeg, result.Kind);
    }

    [Fact]
    public void Detect_EmptyBroken_ReturnsUnknown()
    {
        var detector = new FileTypeDetector();
        var result = detector.Detect(Resource("broken_no_ext"));
        Assert.Equal(FileKind.Unknown, result.Kind);
    }
}
