using Tomtastisch.FileClassifier;
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

        var evidence = EvidenceHashing.HashBytes(payload, "package-backed.pdf");
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

    [Fact]
    public void PackageContains_CsCoreAssembly_ForCrossLanguageRuntimeContract()
    {
        var csCoreAssembly = System.Reflection.Assembly.Load("FileClassifier.CSCore");
        Assert.Equal("FileClassifier.CSCore", csCoreAssembly.GetName().Name);

        var mapperType = csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Mapping.FileDetectionMapper");
        Assert.NotNull(mapperType);

        var enumUtilityType = csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.EnumUtility");
        var guardUtilityType = csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.GuardUtility");
        var hashNormalizationUtilityType =
            csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.HashNormalizationUtility");
        var materializationUtilityType =
            csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.MaterializationUtility");
        var projectOptionsUtilityType =
            csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.ProjectOptionsUtility");
        var evidencePolicyUtilityType =
            csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.EvidencePolicyUtility");
        var archivePathPolicyUtilityType =
            csCoreAssembly.GetType("Tomtastisch.FileClassifier.CSCore.Utilities.ArchivePathPolicyUtility");
        Assert.NotNull(enumUtilityType);
        Assert.NotNull(guardUtilityType);
        Assert.NotNull(hashNormalizationUtilityType);
        Assert.NotNull(materializationUtilityType);
        Assert.NotNull(projectOptionsUtilityType);
        Assert.NotNull(evidencePolicyUtilityType);
        Assert.NotNull(archivePathPolicyUtilityType);
    }
}
