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
        if (!string.IsNullOrWhiteSpace(state.TempRoot) && Directory.Exists(state.TempRoot))
        {
            Directory.Delete(state.TempRoot, recursive: true);
        }
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

    [Given("ein leeres temporäres Zielverzeichnis")]
    public void GivenAnEmptyTemporaryTargetDirectory()
    {
        var state = State();
        var tempRoot = Path.Combine(Path.GetTempPath(), "ftd-bdd-materialize-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        state.TempRoot = tempRoot;
    }

    [Given("ich lese die Datei {string} als aktuelle Bytes")]
    public void GivenIReadFileAsCurrentBytes(string name)
    {
        var path = TestResources.Resolve(name);
        Assert.True(File.Exists(path), $"Test resource missing: {path}");
        State().CurrentPayload = File.ReadAllBytes(path);
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

    [When("ich extrahiere die ZIP-Datei sicher in Memory")]
    public void WhenIExtractZipFileSafelyToMemory()
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.CurrentPath));
        var entries = ZipProcessing.ExtractToMemory(state.CurrentPath!, verifyBeforeExtract: true);
        state.LastExtractedEntries = entries;
    }

    [When("ich übernehme den ersten extrahierten Eintrag als aktuelle Bytes")]
    public void WhenIUseFirstExtractedEntryAsCurrentBytes()
    {
        var state = State();
        Assert.NotNull(state.LastExtractedEntries);
        Assert.NotEmpty(state.LastExtractedEntries!);
        state.CurrentPayload = state.LastExtractedEntries![0].Content.ToArray();
    }

    [When("ich speichere die aktuellen Bytes als {string}")]
    public void WhenIPersistCurrentBytesAs(string fileName)
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));

        var destination = Path.Combine(state.TempRoot!, fileName);
        var ok = FileMaterializer.Persist(state.CurrentPayload!, destination, overwrite: false, secureExtract: false);
        Assert.True(ok);
        state.LastMaterializedPath = destination;
    }

    [When("ich lade die zuletzt gespeicherten Bytes als aktuelle Bytes")]
    public void WhenILoadLastMaterializedBytesAsCurrentBytes()
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.LastMaterializedPath));
        Assert.True(File.Exists(state.LastMaterializedPath), $"File missing: {state.LastMaterializedPath}");
        state.CurrentPayload = File.ReadAllBytes(state.LastMaterializedPath!);
    }

    [When("ich den Dateityp mit Endungspruefung ermittle")]
    public void WhenIDetectTheFileTypeWithExtensionVerification()
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.CurrentPath));

        var detector = new FileTypeDetector();
        state.LastResult = detector.Detect(state.CurrentPath!, verifyExtension: true);
    }

    [When("ich die Endung gegen den erkannten Typ pruefe")]
    public void WhenIVerifyExtensionAgainstDetectedType()
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.CurrentPath));

        var detector = new FileTypeDetector();
        state.ExtensionMatchResult = detector.DetectAndVerifyExtension(state.CurrentPath!);
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

    [Then("ist das Endungsergebnis {string}")]
    public void ThenTheExtensionResultIs(string expectedBoolean)
    {
        var state = State();
        Assert.NotNull(state.ExtensionMatchResult);
        Assert.True(bool.TryParse(expectedBoolean, out var expected), $"Expected boolean literal but got: {expectedBoolean}");
        Assert.Equal(expected, state.ExtensionMatchResult.Value);
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

    [Then("ist der extrahierte Eintragssatz nicht leer")]
    public void ThenExtractedEntrySetIsNotEmpty()
    {
        var state = State();
        Assert.NotNull(state.LastExtractedEntries);
        Assert.NotEmpty(state.LastExtractedEntries!);
    }

    [Then("existiert die gespeicherte Datei {string}")]
    public void ThenMaterializedFileExists(string fileName)
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));
        var path = Path.Combine(state.TempRoot!, fileName);
        Assert.True(File.Exists(path), $"File missing: {path}");
    }

    [Then("entspricht die gespeicherte Datei {string} den aktuellen Bytes")]
    public void ThenMaterializedFileEqualsCurrentBytes(string fileName)
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));

        var path = Path.Combine(state.TempRoot!, fileName);
        Assert.True(File.Exists(path), $"File missing: {path}");
        Assert.Equal(state.CurrentPayload!, File.ReadAllBytes(path));
    }

    private DetectionScenarioState State() => _scenarioContext.Get<DetectionScenarioState>(StateKey);
}
