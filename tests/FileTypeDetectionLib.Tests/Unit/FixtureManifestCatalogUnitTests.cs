using System.IO;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FixtureManifestCatalogUnitTests
{
    [Fact]
    public void Resolve_AcceptsFixtureId_AndFileName_ForSameResource()
    {
        var byId = TestResources.Resolve("fx.sample_zip");
        var byName = TestResources.Resolve("sample.zip");

        Assert.Equal(byName, byId);
        Assert.True(File.Exists(byId));
    }

    [Fact]
    public void Describe_ReturnsStableMetadata_ForArchiveFixtures()
    {
        var sevenZip = TestResources.Describe("fx.sample_7z");
        var rar = TestResources.Describe("fx.sample_rar");

        Assert.Equal("archive/7z", sevenZip.DataType);
        Assert.Equal("archive/rar", rar.DataType);
        Assert.StartsWith("sha256:", sevenZip.ObjectId);
        Assert.StartsWith("sha256:", rar.ObjectId);
    }
}