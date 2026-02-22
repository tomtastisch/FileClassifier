using System.IO;
using System.IO.Compression;
using System.Reflection;
using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;
using Tomtastisch.FileClassifier.Infrastructure.Utils;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class CsCoreUtilityBridgeUnitTests
{
    private enum ProbeEnum
    {
        Third = 30,
        First = 10,
        Second = 20
    }

    [Fact]
    public void CsCoreBridge_UsesCsCoreWhenAvailable_OrFallsBackFailClosed()
    {
        if (CsCoreRuntimeBridge.IsCsCoreAvailable)
        {
            var csCoreAssembly = Assembly.Load("FileClassifier.CSCore");
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.EnumUtility"));
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.IterableUtility"));
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.GuardUtility"));
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.ExceptionFilterUtility"));
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.HashNormalizationUtility"));
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.MaterializationUtility"));
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.ProjectOptionsUtility"));
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.EvidencePolicyUtility"));
            Assert.NotNull(csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.ArchivePathPolicyUtility"));
            return;
        }

        var values = EnumUtils.GetValues<ProbeEnum>();
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void EnumUtils_GetValues_AscendingSlice_RemainsDeterministic()
    {
        var values = EnumUtils.GetValues<ProbeEnum>(EnumUtils.EnumSortOrder.Ascending, fromIndex: 1, toIndex: 2);
        Assert.Equal(new[] { ProbeEnum.Second, ProbeEnum.Third }, values);
    }

    [Fact]
    public void IterableUtils_CloneArray_ReturnsDefensiveCopy_AndPreservesNull()
    {
        var source = new[] { "alpha", "beta" };
        var clone = IterableUtils.CloneArray(source);

        Assert.NotNull(clone);
        Assert.NotSame(source, clone);
        Assert.Equal(source, clone);

        source[0] = "changed";
        Assert.Equal("alpha", clone[0]);

        string[]? nullSource = null;
        var nullClone = IterableUtils.CloneArray(nullSource);
        Assert.Null(nullClone);
    }

    [Fact]
    public void ArgumentGuard_FailClosedContracts_RemainStable()
    {
        ArgumentGuard.NotNothing("value", "value");
        ArgumentGuard.RequireLength(new[] { 1, 2 }, 2, "items");
        ArgumentGuard.EnumDefined(typeof(ProbeEnum), ProbeEnum.First, "value");

        Assert.Throws<ArgumentNullException>(() => ArgumentGuard.NotNothing<string>(null!, "value"));
        Assert.Throws<ArgumentException>(() => ArgumentGuard.RequireLength(new[] { 1 }, 2, "items"));
        Assert.Throws<ArgumentException>(() => ArgumentGuard.EnumDefined(typeof(string), "x", "value"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ArgumentGuard.EnumDefined(typeof(ProbeEnum), (ProbeEnum)999, "value"));
    }

    [Fact]
    public void ExceptionFilterGuard_ExpectedSets_RemainStable()
    {
        Assert.True(ExceptionFilterGuard.IsArchiveValidationException(new InvalidDataException("invalid")));
        Assert.True(ExceptionFilterGuard.IsPathNormalizationException(new UnauthorizedAccessException("denied")));
        Assert.True(ExceptionFilterGuard.IsPathResolutionException(new PathTooLongException("too long")));
        Assert.True(ExceptionFilterGuard.IsLoggerWriteException(new FormatException("format")));
        Assert.False(ExceptionFilterGuard.IsPathNormalizationException(new FormatException("format")));
    }

    [Fact]
    public void BridgeTelemetry_TracksDelegationOrFallback_ForHashAndMaterializationPaths()
    {
        CsCoreRuntimeBridge.ResetTelemetry();

        var originalOptions = FileTypeOptions.GetSnapshot();
        var originalHmacEnv = Environment.GetEnvironmentVariable("FILECLASSIFIER_HMAC_KEY_B64");
        try
        {
            Environment.SetEnvironmentVariable("FILECLASSIFIER_HMAC_KEY_B64", "invalid-base64");

            _ = new HashDigestSet(" A ", " B ", " C ", " D ", " E ", " F ", hasPhysicalHash: true, hasLogicalHash: true);
            _ = HashDigestSet.Empty;
            _ = HashOptions.Normalize(new HashOptions { MaterializedFileName = "../nested/evidence.bin" });

            var secureEvidence = EvidenceHashing.HashBytes(
                new byte[] { 0xAB, 0xCD },
                "   ",
                new HashOptions { IncludeSecureHash = true });
            Assert.Equal("payload.bin", secureEvidence.Label);
            Assert.Contains("invalid Base64", secureEvidence.Notes, StringComparison.Ordinal);

            var normalizedPath = string.Empty;
            var isDirectoryPath = false;
            var normalizePathOk = ArchiveEntryPathPolicy.TryNormalizeRelativePath(
                "folder/sub/file.bin",
                allowDirectoryMarker: false,
                normalizedPath: ref normalizedPath,
                isDirectory: ref isDirectoryPath);
            Assert.True(normalizePathOk);
            Assert.Equal("folder/sub/file.bin", normalizedPath);
            Assert.False(isDirectoryPath);

            var rootPath = Path.GetPathRoot(Path.GetTempPath());
            Assert.False(string.IsNullOrWhiteSpace(rootPath));
            Assert.True(DestinationPathGuard.IsRootPath(rootPath!));

            var invalidOptions = new FileTypeProjectOptions
            {
                MaxBytes = 8,
                SniffBytes = 0,
                MaxZipEntries = 0,
                MaxZipTotalUncompressedBytes = 0,
                MaxZipEntryUncompressedBytes = 0,
                MaxZipCompressionRatio = -1,
                MaxZipNestingDepth = -1,
                MaxZipNestedBytes = 0,
                DeterministicHash = new HashOptions { MaterializedFileName = "../nested/options.bin" }
            };

            FileTypeOptions.SetSnapshot(invalidOptions);
            var normalizedOptions = FileTypeOptions.GetSnapshot();
            Assert.True(normalizedOptions.MaxBytes >= 1);
            Assert.Equal("options.bin", normalizedOptions.DeterministicHash.MaterializedFileName);

            using var tempRoot = TestTempPaths.CreateScope("ftd-cscore-bridge");
            var destination = Path.Combine(tempRoot.RootPath, "payload.bin");
            var ok = FileMaterializer.Persist(new byte[] { 0x10, 0x20, 0x30 }, destination, overwrite: false, secureExtract: false);
            Assert.True(ok);

            var extensionMismatchPath = Path.Combine(tempRoot.RootPath, "mismatch.txt");
            File.Copy(TestResources.Resolve("sample.pdf"), extensionMismatchPath);
            var extensionDetail = new FileTypeDetector().DetectDetailed(extensionMismatchPath, verifyExtension: true);
            Assert.Equal(FileKind.Unknown, extensionDetail.DetectedType.Kind);

            var lockedPath = Path.Combine(tempRoot.RootPath, "locked.bin");
            File.WriteAllBytes(lockedPath, "%PDF-"u8.ToArray());
            using (var locked = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var ioDetail = new FileTypeDetector().DetectDetailed(lockedPath);
                Assert.Equal("ExceptionIO", ioDetail.ReasonCode);
            }

            using (var openDocumentStream = new MemoryStream(CreateOpenDocumentPackage("application/vnd.oasis.opendocument.spreadsheet"), writable: false))
            {
                var openDocumentType = OpenXmlRefiner.TryRefineStream(openDocumentStream);
                Assert.Equal(FileKind.Xls, openDocumentType.Kind);
            }

            var legacyPayload = CreateOleLikePayload("WordDocument");
            var legacyType = LegacyOfficeBinaryRefiner.TryRefineBytes(legacyPayload);
            Assert.Equal(FileKind.Doc, legacyType.Kind);

            var snapshot = CsCoreRuntimeBridge.GetTelemetrySnapshot();

            Assert.True(snapshot.GetTotalCount("NormalizeDigest") > 0);
            Assert.True(snapshot.GetTotalCount("CreateEmptyDigestParts") > 0);
            Assert.True(snapshot.GetTotalCount("NormalizeMaterializedFileName") > 0);
            Assert.True(snapshot.GetTotalCount("IsPayloadWithinMaxBytes") > 0);
            Assert.True(snapshot.GetTotalCount("DecideMaterializationMode") > 0);
            Assert.True(snapshot.GetTotalCount("NormalizeProjectOptionsValues") > 0);
            Assert.True(snapshot.GetTotalCount("BuildSummaryValues") > 0);
            Assert.True(snapshot.GetTotalCount("IsExtensionMatch") > 0);
            Assert.True(snapshot.GetTotalCount("ExceptionToReasonCode") > 0);
            Assert.True(snapshot.GetTotalCount("ResolveOpenDocumentMimeKindKey") > 0);
            Assert.True(snapshot.GetTotalCount("ResolveArchivePackageKindKey") > 0);
            Assert.True(snapshot.GetTotalCount("ResolveLegacyMarkerKindKey") > 0);
            Assert.True(snapshot.GetTotalCount("NormalizeEvidenceLabel") > 0);
            Assert.True(snapshot.GetTotalCount("AppendNoteIfAny") > 0);
            Assert.True(snapshot.GetTotalCount("ResolveHmacKeyFromEnvironment") > 0);
            Assert.True(snapshot.GetTotalCount("NormalizeArchiveRelativePath") > 0);
            Assert.True(snapshot.GetTotalCount("IsRootPath") > 0);

            if (snapshot.IsCsCoreAvailable)
            {
                Assert.True(snapshot.TotalDelegated > 0);
            }
            else
            {
                Assert.True(snapshot.TotalFallback > 0);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("FILECLASSIFIER_HMAC_KEY_B64", originalHmacEnv);
            FileTypeOptions.SetSnapshot(originalOptions);
        }
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

    private static byte[] CreateOleLikePayload(params string[] markers)
    {
        var payload = new byte[1024];
        var oleSignature = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        Buffer.BlockCopy(oleSignature, 0, payload, 0, oleSignature.Length);

        var offset = 256;
        foreach (var marker in markers)
        {
            var markerBytes = System.Text.Encoding.ASCII.GetBytes(marker);
            Buffer.BlockCopy(markerBytes, 0, payload, offset, markerBytes.Length);
            offset += markerBytes.Length + 8;
        }

        return payload;
    }
}
