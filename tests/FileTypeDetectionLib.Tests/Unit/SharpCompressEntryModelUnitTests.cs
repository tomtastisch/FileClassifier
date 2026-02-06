using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class SharpCompressEntryModelUnitTests
{
    [Fact]
    public void Properties_ReturnDefaults_WhenEntryIsNull()
    {
        var model = new SharpCompressEntryModel(null!);

        Assert.Equal(string.Empty, model.RelativePath);
        Assert.False(model.IsDirectory);
        Assert.Null(model.UncompressedSize);
        Assert.Null(model.CompressedSize);
        Assert.Equal(string.Empty, model.LinkTarget);
        Assert.NotNull(model.OpenStream());
    }
}
