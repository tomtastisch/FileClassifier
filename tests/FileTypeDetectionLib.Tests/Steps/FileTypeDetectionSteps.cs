using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Reqnroll;
using Xunit;

namespace FileTypeDetectionLib.Tests.Steps;

[Binding]
public sealed class FileTypeDetectionSteps
{
    private const string StateKey = "detection_state";
    private readonly ScenarioContext _scenarioContext;

    public FileTypeDetectionSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario(Order = 0)]
    public void InitializeScenario()
    {
        _scenarioContext.Set(new DetectionScenarioState(), StateKey);
    }

    [AfterScenario(Order = 1000)]
    public void CleanupScenario()
    {
        var state = State();
        FileTypeDetector.SetDefaultOptions(state.OriginalOptions);
    }

    [Given("die Ressource {string} existiert")]
    public void GivenTheResourceExists(string name)
    {
        var path = TestResources.Resolve(name);
        Assert.True(File.Exists(path), $"Test resource missing: {path}");
    }

    [Given("die Datei {string}")]
    public void GivenTheFile(string name)
    {
        var path = TestResources.Resolve(name);
        Assert.True(File.Exists(path), $"Test resource missing: {path}");
        State().CurrentPath = path;
    }

    [Given("die maximale Dateigroesse ist {long} Bytes")]
    public void GivenTheMaximumSizeInBytes(long maxBytes)
    {
        var options = FileTypeDetector.GetDefaultOptions();
        options.MaxBytes = maxBytes;
        FileTypeDetector.SetDefaultOptions(options);
    }

    [When("ich den Dateityp ermittle")]
    public void WhenIDetectTheFileType()
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.CurrentPath));

        var detector = new FileTypeDetector();
        state.LastResult = detector.Detect(state.CurrentPath!);
    }

    [Then("ist der erkannte Typ {string}")]
    public void ThenTheDetectedKindIs(string expectedKind)
    {
        var state = State();
        Assert.NotNull(state.LastResult);

        Assert.True(
            Enum.TryParse<FileKind>(expectedKind, ignoreCase: true, out var expected),
            $"Unknown FileKind literal in feature: {expectedKind}");

        Assert.Equal(expected, state.LastResult!.Kind);
    }

    [Then("ist der MIME-Provider build-konform aktiv")]
    public void ThenTheMimeProviderIsBuildConform()
    {
#if USE_ASPNETCORE_MIME
        const string expectedBackend = "AspNetCore";
#else
        const string expectedBackend = "HeyRedMime";
#endif
        Assert.Equal(expectedBackend, MimeProviderDiagnostics.ActiveBackendName);
    }

    private DetectionScenarioState State() => _scenarioContext.Get<DetectionScenarioState>(StateKey);
}
