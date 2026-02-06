using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveProcessingFacadeUnitTests
{
    public static TheoryData<string> ArchiveFixtures => new()
    {
        "fx.sample_zip",
        "fx.sample_rar",
        "fx.sample_7z"
    };

    [Theory]
    [MemberData(nameof(ArchiveFixtures))]
    public void TryValidate_ReturnsTrue_ForArchiveBytes_AndFalse_ForPdfBytes(string fixtureId)
    {
        var archiveBytes = File.ReadAllBytes(TestResources.Resolve(fixtureId));
        var pdfBytes = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        Assert.True(ArchiveProcessing.TryValidate(archiveBytes));
        Assert.False(ArchiveProcessing.TryValidate(pdfBytes));
    }

    [Theory]
    [MemberData(nameof(ArchiveFixtures))]
    public void TryExtractToMemory_ReturnsEntries_ForValidArchiveBytes(string fixtureId)
    {
        var archiveBytes = File.ReadAllBytes(TestResources.Resolve(fixtureId));

        var entries = ArchiveProcessing.TryExtractToMemory(archiveBytes);

        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.RelativePath));
            Assert.NotEmpty(entry.Content);
        }
    }
}