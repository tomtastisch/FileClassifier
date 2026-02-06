using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class SharpCompressEntryModelUnitTests
{
    [Fact]
    public void Properties_ReturnDefaults_WhenEntryIsNull()
    {
        var model = new SharpCompressEntryModel(null!);

        Assert.Equal(string.Empty, (string?)model.RelativePath);
        Assert.False((bool)model.IsDirectory);
        Assert.Null<long>(model.UncompressedSize);
        Assert.Null<long>(model.CompressedSize);
        Assert.Equal(string.Empty, (string?)model.LinkTarget);
        Assert.NotNull(model.OpenStream());
    }
}