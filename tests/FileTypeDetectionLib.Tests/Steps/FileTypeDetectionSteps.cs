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
    private const string ResourceColumn = "ressource";
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
        TestTempPaths.CleanupTempRoot(state.TempRoot);
    }

    [Given("die Ressource {string} existiert")]
    public void GivenTheResourceExists(string name)
    {
        AssertResourceExists(name);
    }

    [Given("die folgenden Ressourcen existieren")]
    public void GivenTheFollowingResourcesExist(Table table)
    {
        Assert.NotNull(table);
        Assert.NotEmpty(table.Rows);
        Assert.True(table.ContainsColumn(ResourceColumn), $"Expected table column '{ResourceColumn}'.");

        foreach (var row in table.Rows)
        {
            AssertResourceExists(row[ResourceColumn]);
        }
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
        state.TempRoot = TestTempPaths.CreateTempRoot("ftd-bdd-materialize");
    }

    [Given("ich lese die Datei {string} als aktuelle Bytes")]
    public void GivenIReadFileAsCurrentBytes(string name)
    {
        var path = TestResources.Resolve(name);
        Assert.True(File.Exists(path), $"Test resource missing: {path}");
        State().CurrentPayload = File.ReadAllBytes(path);
    }

    [Given("ich erzeuge aktuelle Archiv-Bytes vom Typ {string}")]
    public void GivenICreateCurrentArchiveBytesOfType(string archiveType)
    {
        State().CurrentPayload = CreateArchivePayload(archiveType);
    }

    [Given("es existiert bereits eine gespeicherte Datei {string}")]
    public void GivenExistingMaterializedFile(string fileName)
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));

        var path = Path.Combine(state.TempRoot!, fileName);
        var bytes = new byte[] { 0xAA, 0xBB, 0xCC };
        File.WriteAllBytes(path, bytes);

        state.ExistingFilePath = path;
        state.ExistingFileBytes = bytes;
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

    [When("ich den deterministischen Hashbericht der aktuellen Datei berechne")]
    public void WhenICalculateDeterministicHashReportForCurrentFile()
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.CurrentPath));
        state.LastRoundTripReport = DeterministicHashing.VerifyRoundTrip(state.CurrentPath!);
    }

    [When("ich den deterministischen Hash der aktuellen Bytes berechne")]
    public void WhenICalculateDeterministicHashForCurrentBytes()
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        state.LastHashEvidence = DeterministicHashing.HashBytes(state.CurrentPayload!, "bdd-current-bytes");
    }

    [When("ich den Dateityp der aktuellen Bytes ermittle")]
    public void WhenIDetectTheCurrentPayloadType()
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);

        var detector = new FileTypeDetector();
        state.LastResult = detector.Detect(state.CurrentPayload!);
    }

    [When("ich die aktuellen Archiv-Bytes validiere")]
    public void WhenIValidateCurrentArchiveBytes()
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        state.LastArchiveValidateResult = ZipProcessing.TryValidate(state.CurrentPayload!);
    }

    [When("ich extrahiere die ZIP-Datei sicher in Memory")]
    [When("ich die ZIP-Datei sicher in den Speicher extrahiere")]
    public void WhenIExtractZipFileSafelyToMemory()
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.CurrentPath));
        var entries = ZipProcessing.ExtractToMemory(state.CurrentPath!, verifyBeforeExtract: true);
        state.LastExtractedEntries = entries;
    }

    [When("ich die aktuellen Archiv-Bytes sicher in den Speicher extrahiere")]
    public void WhenIExtractCurrentArchiveBytesSafelyToMemory()
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        state.LastExtractedEntries = ZipProcessing.TryExtractToMemory(state.CurrentPayload!);
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
    [When("ich die aktuellen Bytes als {string} speichere")]
    public void WhenIPersistCurrentBytesAs(string fileName)
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));

        var destination = Path.Combine(state.TempRoot!, fileName);
        var ok = FileMaterializer.Persist(state.CurrentPayload!, destination, overwrite: false, secureExtract: false);
        Assert.True(ok);
        state.LastPersistResult = ok;
        state.LastMaterializedPath = destination;
    }

    [When("ich versuche die aktuellen Bytes als {string} ohne overwrite zu speichern")]
    [When("ich versuche, die aktuellen Bytes als {string} ohne overwrite zu speichern")]
    public void WhenITryPersistCurrentBytesWithoutOverwrite(string fileName)
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));

        var destination = Path.Combine(state.TempRoot!, fileName);
        state.LastPersistResult = FileMaterializer.Persist(state.CurrentPayload!, destination, overwrite: false, secureExtract: false);
        state.LastMaterializedPath = destination;
    }

    [When("ich versuche die aktuellen Bytes in den Zielpfad {string} zu speichern")]
    [When("ich versuche, die aktuellen Bytes in den Zielpfad {string} zu speichern")]
    public void WhenITryPersistCurrentBytesToRawDestination(string destinationPath)
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        state.LastPersistResult = FileMaterializer.Persist(state.CurrentPayload!, destinationPath, overwrite: false, secureExtract: false);
        state.LastMaterializedPath = destinationPath;
    }

    [When("ich die aktuellen Archiv-Bytes sicher als Verzeichnis {string} materialisiere")]
    public void WhenIMaterializeCurrentArchiveBytesSecurely(string directoryName)
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));

        var destination = Path.Combine(state.TempRoot!, directoryName);
        state.LastPersistResult = FileMaterializer.Persist(state.CurrentPayload!, destination, overwrite: false, secureExtract: true);
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

    [When("ich die Datei sicher in Bytes lese")]
    public void WhenIReadTheFileSafelyAsBytes()
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.CurrentPath));
        state.LastSafeReadBytes = FileTypeDetector.ReadFileSafe(state.CurrentPath!);
    }

    [When("ich pruefe ob die aktuellen Bytes vom Typ {string} sind")]
    public void WhenICheckCurrentBytesType(string expectedKind)
    {
        var state = State();
        Assert.NotNull(state.CurrentPayload);
        Assert.True(
            Enum.TryParse<FileKind>(expectedKind, ignoreCase: true, out var kind),
            $"Unknown FileKind literal in feature: {expectedKind}");

        var detector = new FileTypeDetector();
        state.LastIsOfTypeResult = detector.IsOfType(state.CurrentPayload!, kind);
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


    [Then("ist der sicher gelesene Bytestrom nicht leer")]
    public void ThenSafelyReadByteStreamIsNotEmpty()
    {
        var state = State();
        Assert.NotNull(state.LastSafeReadBytes);
        Assert.NotEmpty(state.LastSafeReadBytes!);
    }

    [Then("ist das Typpruefungsergebnis {string}")]
    public void ThenTypeCheckResultIs(string expectedBoolean)
    {
        var state = State();
        Assert.NotNull(state.LastIsOfTypeResult);
        Assert.True(bool.TryParse(expectedBoolean, out var expected), $"Expected boolean literal but got: {expectedBoolean}");
        Assert.Equal(expected, state.LastIsOfTypeResult.Value);
    }

    [Then("ist das Archiv-Validierungsergebnis {string}")]
    public void ThenArchiveValidationResultIs(string expectedBoolean)
    {
        var state = State();
        Assert.NotNull(state.LastArchiveValidateResult);
        Assert.True(bool.TryParse(expectedBoolean, out var expected), $"Expected boolean literal but got: {expectedBoolean}");
        Assert.Equal(expected, state.LastArchiveValidateResult.Value);
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

    [Then("enthaelt das materialisierte Verzeichnis {string} mindestens eine Datei")]
    public void ThenMaterializedDirectoryContainsAtLeastOneFile(string directoryName)
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));
        var path = Path.Combine(state.TempRoot!, directoryName);
        Assert.True(Directory.Exists(path), $"Directory missing: {path}");
        Assert.NotEmpty(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
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

    [Then("ist der letzte Speicherversuch fehlgeschlagen")]
    public void ThenLastPersistAttemptFailed()
    {
        var state = State();
        Assert.NotNull(state.LastPersistResult);
        Assert.False(state.LastPersistResult!.Value);
    }

    [Then("ist der letzte Speicherversuch erfolgreich")]
    public void ThenLastPersistAttemptSucceeded()
    {
        var state = State();
        Assert.NotNull(state.LastPersistResult);
        Assert.True(state.LastPersistResult!.Value);
    }

    [Then("ist der Hashbericht logisch konsistent")]
    public void ThenHashReportIsLogicallyConsistent()
    {
        var state = State();
        Assert.NotNull(state.LastRoundTripReport);
        Assert.True(state.LastRoundTripReport!.LogicalConsistent);
        Assert.True(state.LastRoundTripReport.LogicalH1EqualsH2);
        Assert.True(state.LastRoundTripReport.LogicalH1EqualsH3);
        Assert.True(state.LastRoundTripReport.LogicalH1EqualsH4);
    }

    [Then("ist der Hashbericht als Archiv klassifiziert {string}")]
    public void ThenHashReportArchiveClassificationIs(string expectedBoolean)
    {
        var state = State();
        Assert.NotNull(state.LastRoundTripReport);
        Assert.True(bool.TryParse(expectedBoolean, out var expected), $"Expected boolean literal but got: {expectedBoolean}");
        Assert.Equal(expected, state.LastRoundTripReport!.IsArchiveInput);
    }

    [Then("ist im letzten Hashnachweis ein logischer Hash vorhanden")]
    public void ThenLastHashEvidenceContainsLogicalDigest()
    {
        var state = State();
        Assert.NotNull(state.LastHashEvidence);
        Assert.True(state.LastHashEvidence!.Digests.HasLogicalHash);
        Assert.False(string.IsNullOrWhiteSpace(state.LastHashEvidence.Digests.LogicalSha256));
    }

    [Then("ist im letzten Hashnachweis ein physischer Hash vorhanden")]
    public void ThenLastHashEvidenceContainsPhysicalDigest()
    {
        var state = State();
        Assert.NotNull(state.LastHashEvidence);
        Assert.True(state.LastHashEvidence!.Digests.HasPhysicalHash);
        Assert.False(string.IsNullOrWhiteSpace(state.LastHashEvidence.Digests.PhysicalSha256));
    }

    [Then("entsprechen sich logischer und physischer Hash im letzten Nachweis {string}")]
    public void ThenLogicalAndPhysicalDigestEqualityIs(string expectedBoolean)
    {
        var state = State();
        Assert.NotNull(state.LastHashEvidence);
        var evidence = state.LastHashEvidence!;
        Assert.True(bool.TryParse(expectedBoolean, out var expected), $"Expected boolean literal but got: {expectedBoolean}");

        var actual = evidence.Digests.HasLogicalHash &&
                     evidence.Digests.HasPhysicalHash &&
                     string.Equals(evidence.Digests.LogicalSha256, evidence.Digests.PhysicalSha256, StringComparison.Ordinal);
        Assert.Equal(expected, actual);
    }

    [Then("bleibt die bestehende Datei {string} unveraendert")]
    public void ThenExistingFileRemainsUnchanged(string fileName)
    {
        var state = State();
        Assert.NotNull(state.ExistingFileBytes);
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));

        var path = Path.Combine(state.TempRoot!, fileName);
        Assert.True(File.Exists(path), $"File missing: {path}");
        Assert.Equal(state.ExistingFileBytes!, File.ReadAllBytes(path));
    }

    [Then("existiert keine Datei im Zielpfad {string}")]
    public void ThenNoFileExistsAtRawDestination(string destinationPath)
    {
        Assert.False(File.Exists(destinationPath), $"Unexpected file exists: {destinationPath}");
    }

    [Then("existiert keine Datei im ungueltigen Zielpfad")]
    public void ThenNoFileExistsAtInvalidDestination()
    {
        var state = State();
        if (string.IsNullOrWhiteSpace(state.LastMaterializedPath))
        {
            return;
        }

        Assert.False(File.Exists(state.LastMaterializedPath), $"Unexpected file exists: {state.LastMaterializedPath}");
    }

    [Then("existiert die gespeicherte Datei {string} nicht")]
    public void ThenMaterializedFileDoesNotExist(string fileName)
    {
        var state = State();
        Assert.False(string.IsNullOrWhiteSpace(state.TempRoot));
        var path = Path.Combine(state.TempRoot!, fileName);
        Assert.False(File.Exists(path), $"Unexpected file exists: {path}");
    }

    private DetectionScenarioState State() => _scenarioContext.Get<DetectionScenarioState>(StateKey);

    private static void AssertResourceExists(string name)
    {
        var path = TestResources.Resolve(name);
        Assert.True(File.Exists(path), $"Test resource missing: {path}");
    }

    private static byte[] CreateArchivePayload(string archiveType)
    {
        var normalized = (archiveType ?? string.Empty).Trim().ToLowerInvariant();
        const string entryPath = "inner/note.txt";
        const string content = "hello";

        return normalized switch
        {
            "zip" => ArchivePayloadFactory.CreateZipWithSingleEntry(entryPath, content),
            "tar" => ArchivePayloadFactory.CreateTarWithSingleEntry(entryPath, content),
            "tar.gz" => ArchivePayloadFactory.CreateTarGzWithSingleEntry(entryPath, content),
            "7z" => File.ReadAllBytes(TestResources.Resolve("fx.sample_7z")),
            "rar" => File.ReadAllBytes(TestResources.Resolve("fx.sample_rar")),
            _ => throw new InvalidOperationException($"Unsupported archive type literal in feature: {archiveType}")
        };
    }
}
