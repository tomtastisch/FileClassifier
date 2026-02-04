using System.Text.Json;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeOptionsFacadeUnitTests
{
    [Fact]
    public void LoadOptions_AppliesPartialJson_AndKeepsRemainingDefaults()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            var ok = FileTypeOptions.LoadOptions("{\"maxZipEntries\":1234}");
            var snapshot = FileTypeOptions.GetSnapshot();
            var defaults = FileTypeDetectorOptions.DefaultOptions();

            Assert.True(ok);
            Assert.Equal(1234, snapshot.MaxZipEntries);
            Assert.Equal(defaults.MaxBytes, snapshot.MaxBytes);
            Assert.Equal(defaults.MaxZipCompressionRatio, snapshot.MaxZipCompressionRatio);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void GetOptions_ReturnsCurrentSnapshotAsJson()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            FileTypeOptions.LoadOptions("{\"maxBytes\":1048576,\"maxZipNestingDepth\":4}");

            var json = FileTypeOptions.GetOptions();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal(1048576, root.GetProperty("maxBytes").GetInt64());
            Assert.Equal(4, root.GetProperty("maxZipNestingDepth").GetInt32());
            Assert.True(root.TryGetProperty("headerOnlyNonZip", out _));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }
}
