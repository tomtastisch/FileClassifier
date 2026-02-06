using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Property;

public sealed class FileTypeOptionsPropertyTests
{
    [Fact]
    public void LoadOptions_PreservesSafetyInvariants_ForDeterministicRandomInputs()
    {
        var original = FileTypeOptions.GetSnapshot();
        var rng = new Random(20260204);

        try
        {
            for (var i = 0; i < 100; i++)
            {
                var maxBytes = rng.NextInt64(-2_000_000, 2_000_000);
                var sniffBytes = rng.Next(-20_000, 20_000);
                var maxZipEntries = rng.Next(-500, 500);
                var maxZipEntryBytes = rng.NextInt64(-2_000_000, 2_000_000);
                var maxZipTotalBytes = rng.NextInt64(-4_000_000, 4_000_000);
                var maxZipRatio = rng.Next(-100, 100);
                var maxZipDepth = rng.Next(-10, 10);
                var maxZipNestedBytes = rng.NextInt64(-2_000_000, 2_000_000);
                var headerOnlyNonZip = rng.Next(0, 2) == 0 ? "true" : "false";

                var json = $$"""
                             {"headerOnlyNonZip":{{headerOnlyNonZip}},"maxBytes":{{maxBytes}},"sniffBytes":{{sniffBytes}},"maxZipEntries":{{maxZipEntries}},"maxZipEntryUncompressedBytes":{{maxZipEntryBytes}},"maxZipTotalUncompressedBytes":{{maxZipTotalBytes}},"maxZipCompressionRatio":{{maxZipRatio}},"maxZipNestingDepth":{{maxZipDepth}},"maxZipNestedBytes":{{maxZipNestedBytes}}}
                             """;
                Assert.True(FileTypeOptions.LoadOptions(json));

                var snapshot = FileTypeOptions.GetSnapshot();
                var expectedHeaderOnly = string.Equals(headerOnlyNonZip, "true", StringComparison.Ordinal);
                Assert.Equal(expectedHeaderOnly, snapshot.HeaderOnlyNonZip);
                Assert.True(snapshot.MaxBytes > 0);
                Assert.True(snapshot.SniffBytes > 0);
                Assert.True(snapshot.MaxZipEntries > 0);
                Assert.True(snapshot.MaxZipEntryUncompressedBytes > 0);
                Assert.True(snapshot.MaxZipTotalUncompressedBytes > 0);
                Assert.True(snapshot.MaxZipCompressionRatio >= 0);
                Assert.True(snapshot.MaxZipNestingDepth >= 0);
                Assert.True(snapshot.MaxZipNestedBytes > 0);
                Assert.NotNull(snapshot.DeterministicHash);
                Assert.False(string.IsNullOrWhiteSpace(snapshot.DeterministicHash.MaterializedFileName));
            }
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }
}