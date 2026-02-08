using FileTypeDetection;
using Xunit;

namespace PackageBacked.Tests;

public sealed class PackageBackedContractTests
{
    [Fact]
    public void Detect_And_Hash_Contracts_RemainStable()
    {
        var payload = "%PDF-1.7\npackage-backed\n"u8.ToArray();
        var detector = new FileTypeDetector();
        var detected = detector.Detect(payload);

        Assert.Equal(FileKind.Pdf, detected.Kind);
        Assert.Equal(".pdf", detected.CanonicalExtension);

        var evidence = DeterministicHashing.HashBytes(payload, "package-backed.pdf");
        Assert.True(evidence.Digests.HasPhysicalHash);
        Assert.True(evidence.Digests.HasLogicalHash);
        Assert.Equal(FileKind.Pdf, evidence.DetectedType.Kind);
    }

    [Fact]
    public void Materializer_Persists_Bytes_FailClosedContract()
    {
        var payload = "package-backed-persist"u8.ToArray();
        var outputDir = Path.Combine(Path.GetTempPath(), "ftd-package-backed");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, $"persist-{Guid.NewGuid():N}.bin");

        var ok = FileMaterializer.Persist(payload, outputPath, overwrite: false, secureExtract: false);

        Assert.True(ok);
        Assert.True(File.Exists(outputPath));
        Assert.Equal(payload, File.ReadAllBytes(outputPath));
    }
}
