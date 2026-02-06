using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class MimeProviderUnitTests
{
    [Fact]
    public void GetMime_ReturnsEmpty_ForNullOrWhitespace()
    {
        Assert.Equal(string.Empty, (string?)MimeProvider.GetMime(null));
        Assert.Equal(string.Empty, (string?)MimeProvider.GetMime(""));
        Assert.Equal(string.Empty, (string?)MimeProvider.GetMime("   "));
    }

    [Fact]
    public void GetMime_HandlesDotPrefix_Consistently()
    {
        var withDot = MimeProvider.GetMime(".txt");
        var withoutDot = MimeProvider.GetMime("txt");

        Assert.Equal((string?)withDot, (string?)withoutDot);
        Assert.False(string.IsNullOrWhiteSpace(withDot));
    }

    [Fact]
    public void ActiveBackendName_IsHeyRedMime_ByDefault()
    {
        Assert.Equal((string?)"HeyRedMime", (string?)MimeProviderDiagnostics.ActiveBackendName);
    }
}
