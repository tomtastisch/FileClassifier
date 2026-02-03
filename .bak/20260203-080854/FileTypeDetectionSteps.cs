using System;
using System.IO;
using FileTypeDetection;
using Reqnroll;
using Xunit;

namespace FileTypeDetectionLib.Tests.Steps;

[Binding]
public sealed class FileTypeDetectionSteps
{
    private string? _path;
    private FileType? _result;

    private static string Resource(string name) =>
        Path.Combine(AppContext.BaseDirectory, "resources", name);

    [Given(@"die Ressource ""(.*)"" existiert")]
    public void GivenTheResourceExists(string name)
    {
        var p = Resource(name);
        Assert.True(File.Exists(p), $"Test resource missing: {p}");
    }

    [Given(@"sei die Datei ""(.*)""")]
    public void GivenTheFile(string name)
    {
        _path = Resource(name);
        Assert.True(File.Exists(_path), $"Test resource missing: {_path}");
    }

    [When(@"ich den Dateityp ermittle")]
    public void WhenIDetectTheFileType()
    {
        var detector = new FileTypeDetector();
        _result = detector.Detect(_path!);
    }

    [Then(@"ist der erkannte Typ ""(.*)""")]
    public void ThenTheDetectedKindIs(string expectedKind)
    {
        Assert.NotNull(_result);

        Assert.True(
            Enum.TryParse<FileKind>(expectedKind, ignoreCase: true, out var expected),
            $"Unknown FileKind literal in feature: {expectedKind}"
        );

        Assert.Equal(expected, _result!.Kind);
    }
}
