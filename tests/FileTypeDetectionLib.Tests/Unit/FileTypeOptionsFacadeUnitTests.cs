using System;
using System.IO;
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
            var defaults = FileTypeProjectOptions.DefaultOptions();

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
            Assert.True(root.TryGetProperty("rejectArchiveLinks", out _));
            Assert.True(root.TryGetProperty("allowUnknownArchiveEntrySize", out _));
            Assert.True(root.TryGetProperty("deterministicHash", out var deterministicHash));
            Assert.True(deterministicHash.TryGetProperty("includeFastHash", out _));
            Assert.True(deterministicHash.TryGetProperty("includePayloadCopies", out _));
            Assert.True(deterministicHash.TryGetProperty("materializedFileName", out _));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void LoadOptions_KeepsDefaults_ForInvalidNumericValues()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            var defaults = FileTypeProjectOptions.DefaultOptions();
            var ok = FileTypeOptions.LoadOptions("{\"maxBytes\":0,\"maxZipEntries\":-1,\"maxZipNestingDepth\":-2}");
            var snapshot = FileTypeOptions.GetSnapshot();

            Assert.True(ok);
            Assert.Equal(defaults.MaxBytes, snapshot.MaxBytes);
            Assert.Equal(defaults.MaxZipEntries, snapshot.MaxZipEntries);
            Assert.Equal(defaults.MaxZipNestingDepth, snapshot.MaxZipNestingDepth);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void LoadOptions_Applies_HeaderOnlyNonZip_Boolean()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            var ok = FileTypeOptions.LoadOptions("{\"headerOnlyNonZip\":false,\"maxBytes\":2048}");
            var snapshot = FileTypeOptions.GetSnapshot();

            Assert.True(ok);
            Assert.False(snapshot.HeaderOnlyNonZip);
            Assert.Equal(2048, snapshot.MaxBytes);

            var json = FileTypeOptions.GetOptions();
            using var doc = JsonDocument.Parse(json);
            Assert.False(doc.RootElement.GetProperty("headerOnlyNonZip").GetBoolean());
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void LoadOptions_Applies_ArchiveSecurityBooleans()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            var ok = FileTypeOptions.LoadOptions(
                "{\"rejectArchiveLinks\":false,\"allowUnknownArchiveEntrySize\":true}");
            var snapshot = FileTypeOptions.GetSnapshot();

            Assert.True(ok);
            Assert.False(snapshot.RejectArchiveLinks);
            Assert.True(snapshot.AllowUnknownArchiveEntrySize);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void LoadOptions_Applies_DeterministicHashObject()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            var ok = FileTypeOptions.LoadOptions(
                "{\"deterministicHash\":{\"includePayloadCopies\":true,\"includeFastHash\":false,\"materializedFileName\":\"reports/evidence.bin\"}}");
            var snapshot = FileTypeOptions.GetSnapshot();

            Assert.True(ok);
            Assert.True(snapshot.DeterministicHash.IncludePayloadCopies);
            Assert.False(snapshot.DeterministicHash.IncludeFastHash);
            Assert.Equal("evidence.bin", snapshot.DeterministicHash.MaterializedFileName);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void LoadOptions_RejectsInvalidJsonAndRoot()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            Assert.False(FileTypeOptions.LoadOptions(null));
            Assert.False(FileTypeOptions.LoadOptions("   "));
            Assert.False(FileTypeOptions.LoadOptions("[1,2,3]"));
            Assert.False(FileTypeOptions.LoadOptions("{invalid-json"));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void LoadOptions_IgnoresUnknownKeys_AndInvalidTypes()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            var defaults = FileTypeProjectOptions.DefaultOptions();
            var ok = FileTypeOptions.LoadOptions(
                "{\"unknownKey\":true,\"maxBytes\":\"oops\",\"deterministicHash\":false}");
            var snapshot = FileTypeOptions.GetSnapshot();

            Assert.True(ok);
            Assert.Equal(defaults.MaxBytes, snapshot.MaxBytes);
            Assert.Equal(defaults.DeterministicHash.MaterializedFileName,
                snapshot.DeterministicHash.MaterializedFileName);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void LoadOptions_Applies_DeterministicHashFlatFields()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            var ok = FileTypeOptions.LoadOptions(
                "{\"deterministicHashIncludePayloadCopies\":true,\"deterministicHashIncludeFastHash\":false,\"deterministicHashMaterializedFileName\":\"x.bin\"}");
            var snapshot = FileTypeOptions.GetSnapshot();

            Assert.True(ok);
            Assert.True(snapshot.DeterministicHash.IncludePayloadCopies);
            Assert.False(snapshot.DeterministicHash.IncludeFastHash);
            Assert.Equal("x.bin", snapshot.DeterministicHash.MaterializedFileName);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void LoadOptionsFromPath_ValidatesExtensionAndExistence()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            Assert.False(FileTypeOptions.LoadOptionsFromPath("   "));
            Assert.False(FileTypeOptions.LoadOptionsFromPath("missing.json"));
            Assert.False(FileTypeOptions.LoadOptionsFromPath("options.txt"));

            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            File.WriteAllText(path, "{\"maxZipEntries\":7}");
            try
            {
                Assert.True(FileTypeOptions.LoadOptionsFromPath(path));
                Assert.Equal(7, FileTypeOptions.GetSnapshot().MaxZipEntries);
            }
            finally
            {
                File.Delete(path);
            }
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void SetSnapshot_NormalizesInvalidNumericValues()
    {
        var original = FileTypeOptions.GetSnapshot();
        try
        {
            var invalid = new FileTypeProjectOptions
            {
                MaxBytes = 0,
                SniffBytes = -1,
                MaxZipEntries = 0,
                MaxZipTotalUncompressedBytes = -10,
                MaxZipEntryUncompressedBytes = 0,
                MaxZipCompressionRatio = -5,
                MaxZipNestingDepth = -3,
                MaxZipNestedBytes = 0,
                DeterministicHash = new DeterministicHashOptions { MaterializedFileName = "   " }
            };

            FileTypeOptions.SetSnapshot(invalid);
            var snapshot = FileTypeOptions.GetSnapshot();

            Assert.Equal(1, snapshot.MaxBytes);
            Assert.Equal(1, snapshot.SniffBytes);
            Assert.Equal(1, snapshot.MaxZipEntries);
            Assert.Equal(1, snapshot.MaxZipTotalUncompressedBytes);
            Assert.Equal(1, snapshot.MaxZipEntryUncompressedBytes);
            Assert.Equal(0, snapshot.MaxZipCompressionRatio);
            Assert.Equal(0, snapshot.MaxZipNestingDepth);
            Assert.Equal(1, snapshot.MaxZipNestedBytes);
            Assert.Equal("deterministic-roundtrip.bin", snapshot.DeterministicHash.MaterializedFileName);
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }
}