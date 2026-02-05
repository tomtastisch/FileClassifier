using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileTypeRegistryUnitTests
{
    [Fact]
    public void Resolve_MapsAllEnumValues_Deterministically()
    {
        foreach (var kind in Enum.GetValues<FileKind>())
        {
            var fileType = FileTypeRegistry.Resolve(kind);
            Assert.NotNull(fileType);
            Assert.Equal(kind, fileType.Kind);

            if (kind == FileKind.Unknown)
            {
                Assert.False(fileType.Allowed);
                Assert.Equal(string.Empty, fileType.CanonicalExtension);
                continue;
            }

            Assert.True(fileType.Allowed);
            Assert.StartsWith(".", fileType.CanonicalExtension, StringComparison.Ordinal);
            Assert.Contains(FileTypeRegistry.NormalizeAlias(kind.ToString()), fileType.Aliases);
        }
    }

    [Fact]
    public void ResolveByAlias_ResolvesEnumNameAlias_ForAllKnownKinds()
    {
        foreach (var kind in Enum.GetValues<FileKind>())
        {
            if (kind == FileKind.Unknown)
            {
                continue;
            }

            var alias = FileTypeRegistry.NormalizeAlias(kind.ToString());
            var resolved = FileTypeRegistry.ResolveByAlias(alias);
            Assert.Equal(kind, resolved.Kind);
        }
    }

    [Fact]
    public void ResolveByAlias_ResolvesLegacyJpegAliases()
    {
        Assert.Equal(FileKind.Jpeg, FileTypeRegistry.ResolveByAlias("jpg").Kind);
        Assert.Equal(FileKind.Jpeg, FileTypeRegistry.ResolveByAlias("jpe").Kind);
        Assert.Equal(FileKind.Jpeg, FileTypeRegistry.ResolveByAlias("jpeg").Kind);
    }

    [Theory]
    [InlineData("zip")]
    [InlineData("tar")]
    [InlineData("tgz")]
    [InlineData("gz")]
    [InlineData("gzip")]
    [InlineData("bz2")]
    [InlineData("bzip2")]
    [InlineData("xz")]
    [InlineData("7z")]
    [InlineData("zz")]
    [InlineData("rar")]
    public void ResolveByAlias_ResolvesArchiveAliasesAsArchive(string alias)
    {
        Assert.Equal(FileKind.Zip, FileTypeRegistry.ResolveByAlias(alias).Kind);
    }
}
