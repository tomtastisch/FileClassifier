using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class MimeProviderUnitTests
{
    [Fact]
    public void GetMime_ReturnsEmpty_ForNullOrWhitespace()
    {
        var provider = MimeProvider.Instance;

        Assert.Equal(string.Empty, provider.GetMime(null));
        Assert.Equal(string.Empty, provider.GetMime(""));
        Assert.Equal(string.Empty, provider.GetMime("   "));
    }

    [Fact]
    public void GetMime_HandlesDotPrefix_Consistently()
    {
        var provider = MimeProvider.Instance;

        var withDot = provider.GetMime(".txt");
        var withoutDot = provider.GetMime("txt");

        Assert.Equal(withDot, withoutDot);
        Assert.False(string.IsNullOrWhiteSpace(withDot));
    }

    [Fact]
    public void ActiveBackendName_IsHeyRedMime_ByDefault()
    {
        Assert.Equal("HeyRedMime", MimeProviderDiagnostics.ActiveBackendName);
    }
}
