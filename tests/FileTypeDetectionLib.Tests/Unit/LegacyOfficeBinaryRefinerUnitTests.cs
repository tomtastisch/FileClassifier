using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class LegacyOfficeBinaryRefinerUnitTests
{
    [Theory]
    [InlineData("WordDocument", FileKind.Doc)]
    [InlineData("Workbook", FileKind.Xls)]
    [InlineData("PowerPoint Document", FileKind.Ppt)]
    public void TryRefineBytes_DetectsLegacyOfficeMarkers(string marker, FileKind expected)
    {
        var payload = CreateOleLikePayload(marker);

        var detected = LegacyOfficeBinaryRefiner.TryRefineBytes(payload);

        Assert.Equal(expected, detected.Kind);
    }

    [Fact]
    public void TryRefineBytes_ReturnsUnknown_ForNonOlePayload()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var detected = LegacyOfficeBinaryRefiner.TryRefineBytes(payload);

        Assert.Equal(FileKind.Unknown, detected.Kind);
    }

    [Fact]
    public void TryRefineBytes_ReturnsUnknown_ForAmbiguousLegacyMarkers()
    {
        var payload = CreateOleLikePayload("WordDocument", "Workbook");

        var detected = LegacyOfficeBinaryRefiner.TryRefineBytes(payload);

        Assert.Equal(FileKind.Unknown, detected.Kind);
    }

    private static byte[] CreateOleLikePayload(params string[] markers)
    {
        var payload = new byte[1024];
        var oleSignature = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        Buffer.BlockCopy(oleSignature, 0, payload, 0, oleSignature.Length);

        var offset = 256;
        foreach (var marker in markers)
        {
            var markerBytes = System.Text.Encoding.ASCII.GetBytes(marker);
            Buffer.BlockCopy(markerBytes, 0, payload, offset, markerBytes.Length);
            offset += markerBytes.Length + 8;
        }

        return payload;
    }
}
