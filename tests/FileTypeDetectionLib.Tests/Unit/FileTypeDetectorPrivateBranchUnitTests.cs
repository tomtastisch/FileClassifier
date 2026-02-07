using System.Reflection;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeDetectorPrivateBranchUnitTests
{
    [Fact]
    public void LoadOptions_ReturnsDefaults_ForWhitespacePath()
    {
        var options = FileTypeDetector.LoadOptions(" ");
        Assert.Equal(FileTypeProjectOptions.DefaultOptions().MaxBytes, options.MaxBytes);
    }

    [Fact]
    public void LoadOptions_ReturnsDefaults_ForExistingNonJsonFile()
    {
        using var scope = TestTempPaths.CreateScope("ftd-options-nonjson");
        var path = Path.Combine(scope.RootPath, "config.txt");
        File.WriteAllText(path, "{}");

        var options = FileTypeDetector.LoadOptions(path);

        Assert.Equal(FileTypeProjectOptions.DefaultOptions().MaxBytes, options.MaxBytes);
    }

    [Fact]
    public void LoadOptions_ReturnsParsedOptions_ForValidJson()
    {
        using var scope = TestTempPaths.CreateScope("ftd-options-json");
        var path = Path.Combine(scope.RootPath, "config.json");
        File.WriteAllText(path, "{ \"maxBytes\": 12345 }");

        var options = FileTypeDetector.LoadOptions(path);

        Assert.Equal(12345, options.MaxBytes);
    }

    [Fact]
    public void ReadFileSafe_ReturnsEmpty_ForWhitespacePath()
    {
        var data = FileTypeDetector.ReadFileSafe(" ");
        Assert.Empty(data);
    }

    [Fact]
    public void TryValidateArchive_ReturnsFalse_ForNonArchiveFile()
    {
        var path = TestResources.Resolve("sample.pdf");

        Assert.False(FileTypeDetector.TryValidateArchive(path));
    }

    [Fact]
    public void TryValidateArchive_ReturnsFalse_ForWhitespacePath()
    {
        Assert.False(FileTypeDetector.TryValidateArchive(" "));
    }

    [Fact]
    public void ExtractArchiveSafe_ReturnsFalse_WhenPayloadEmpty()
    {
        using var scope = TestTempPaths.CreateScope("ftd-extract-empty");
        var path = Path.Combine(scope.RootPath, "empty.zip");
        File.WriteAllBytes(path, Array.Empty<byte>());

        var detector = new FileTypeDetector();
        var ok = detector.ExtractArchiveSafe(path, Path.Combine(scope.RootPath, "out"), false);

        Assert.False(ok);
    }

    [Fact]
    public void ExtractArchiveSafeToMemory_ReturnsEmpty_WhenPayloadEmpty()
    {
        using var scope = TestTempPaths.CreateScope("ftd-extract-empty-mem");
        var path = Path.Combine(scope.RootPath, "empty.zip");
        File.WriteAllBytes(path, Array.Empty<byte>());

        var detector = new FileTypeDetector();
        var entries = detector.ExtractArchiveSafeToMemory(path, false);

        Assert.Empty(entries);
    }

    [Fact]
    public void Detect_ReturnsUnknown_ForEmptyPayload()
    {
        var detector = new FileTypeDetector();
        var detected = detector.Detect(Array.Empty<byte>());

        Assert.Equal(FileKind.Unknown, detected.Kind);
    }

    [Fact]
    public void DetectDetailed_ReturnsUnknown_ForWhitespacePath()
    {
        var detector = new FileTypeDetector();
        var detail = detector.DetectDetailed(" ");

        Assert.Equal(FileKind.Unknown, detail.DetectedType.Kind);
    }

    [Fact]
    public void ValidateArchiveStreamRaw_ReturnsFalse_WhenStreamUnreadable()
    {
        var method = typeof(FileTypeDetector).GetMethod("ValidateArchiveStreamRaw",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-validate-stream");
        var path = Path.Combine(scope.RootPath, "file.bin");
        File.WriteAllBytes(path, new byte[] { 0x01 });

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);

        var ok = TestGuard.Unbox<bool>(method.Invoke(null, new object[] { fs, opt, descriptor }));

        Assert.False(ok);
    }

    [Fact]
    public void FinalizeArchiveDetection_ReturnsZip_ForUnknownRefinement()
    {
        var method = typeof(FileTypeDetector).GetMethod("FinalizeArchiveDetection",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var traceType = typeof(FileTypeDetector).GetNestedType("DetectionTrace", BindingFlags.NonPublic);
        var trace = Activator.CreateInstance(traceType!);
        var opt = FileTypeProjectOptions.DefaultOptions();

        var refined = FileTypeRegistry.Resolve(FileKind.Unknown);
        var result = TestGuard.NotNull(method.Invoke(null, new[] { refined, opt, trace! }) as FileType);

        Assert.Equal(FileKind.Zip, result.Kind);
    }

    [Fact]
    public void FinalizeArchiveDetection_ReturnsRefined_WhenNotUnknown()
    {
        var method = typeof(FileTypeDetector).GetMethod("FinalizeArchiveDetection",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var traceType = typeof(FileTypeDetector).GetNestedType("DetectionTrace", BindingFlags.NonPublic);
        var trace = Activator.CreateInstance(traceType!);
        var opt = FileTypeProjectOptions.DefaultOptions();

        var refined = FileTypeRegistry.Resolve(FileKind.Docx);
        var result = TestGuard.NotNull(method.Invoke(null, new[] { refined, opt, trace! }) as FileType);

        Assert.Equal(FileKind.Docx, result.Kind);
    }

    [Fact]
    public void WarnIfNoDirectContentDetection_SwallowsUnknownAndDirectKinds()
    {
        var method = typeof(FileTypeDetector).GetMethod("WarnIfNoDirectContentDetection",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        method.Invoke(null, new object[] { FileKind.Unknown, FileTypeProjectOptions.DefaultOptions() });
        method.Invoke(null, new object[] { FileKind.Pdf, FileTypeProjectOptions.DefaultOptions() });
    }

    [Fact]
    public void ExtensionMatchesKind_HandlesEmptyAndMismatch()
    {
        var method =
            typeof(FileTypeDetector).GetMethod("ExtensionMatchesKind", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        var okEmpty = TestGuard.Unbox<bool>(method.Invoke(null, new object[] { "file", FileKind.Pdf }));
        var okMismatch = TestGuard.Unbox<bool>(method.Invoke(null, new object[] { "file.docx", FileKind.Pdf }));
        var okAlias = TestGuard.Unbox<bool>(method.Invoke(null, new object[] { "file.jpeg", FileKind.Jpeg }));

        Assert.True(okEmpty);
        Assert.False(okMismatch);
        Assert.True(okAlias);
    }

    [Fact]
    public void ReadHeader_ReturnsEmpty_ForWriteOnlyStream()
    {
        var method = typeof(FileTypeDetector).GetMethod("ReadHeader", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-readheader-write");
        var path = Path.Combine(scope.RootPath, "write.bin");
        File.WriteAllBytes(path, new byte[] { 0x01, 0x02 });

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
        var data = TestGuard.NotNull(method.Invoke(null, new object?[] { fs, 4, 1024L }) as byte[]);

        Assert.Empty(data);
    }

    [Fact]
    public void ReadHeader_ReturnsEmpty_ForZeroLengthFile()
    {
        var method = typeof(FileTypeDetector).GetMethod("ReadHeader", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(method);

        using var scope = TestTempPaths.CreateScope("ftd-readheader-zero");
        var path = Path.Combine(scope.RootPath, "zero.bin");
        File.WriteAllBytes(path, Array.Empty<byte>());

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var data = TestGuard.NotNull(method.Invoke(null, new object?[] { fs, 4, 1024L }) as byte[]);

        Assert.Empty(data);
    }
}
