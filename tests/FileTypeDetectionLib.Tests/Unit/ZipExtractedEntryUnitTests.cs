using System;
using System.IO;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ZipExtractedEntryUnitTests
{
    [Fact]
    public void Constructor_NormalizesNulls_AndComputesSize()
    {
        var entry = new ZipExtractedEntry(null, null);

        Assert.Equal(string.Empty, entry.RelativePath);
        Assert.True(entry.Content.IsDefaultOrEmpty);
        Assert.Equal(0, entry.Size);
    }

    [Fact]
    public void OpenReadOnlyStream_ProvidesReadOnlyBytes()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03 };
        var entry = new ZipExtractedEntry("a.bin", payload);

        using var stream = entry.OpenReadOnlyStream();
        Assert.True(stream.CanRead);
        Assert.False(stream.CanWrite);
        Assert.Equal(payload.Length, stream.Length);

        var roundtrip = new byte[payload.Length];
        var read = stream.Read(roundtrip, 0, roundtrip.Length);
        Assert.Equal(payload.Length, read);
        Assert.Equal(payload, roundtrip);
    }
}
