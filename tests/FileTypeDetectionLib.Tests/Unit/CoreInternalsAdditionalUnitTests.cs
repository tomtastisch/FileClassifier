using System.IO.Compression;
using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class CoreInternalsAdditionalUnitTests
{
    [Fact]
    public void CopyBounded_Throws_WhenLimitExceeded()
    {
        using var input = new MemoryStream(new byte[10]);
        using var output = new MemoryStream();

        Assert.Throws<InvalidOperationException>(() => StreamBounds.CopyBounded(input, output, maxBytes: 5));
    }

    [Fact]
    public void CopyBounded_CopiesWithinLimit()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        using var input = new MemoryStream(payload);
        using var output = new MemoryStream();

        StreamBounds.CopyBounded(input, output, maxBytes: 10);

        Assert.Equal(payload, output.ToArray());
    }

    [Fact]
    public void IsRootPath_DetectsRootAndNonRoot()
    {
        var root = Path.GetPathRoot(Environment.CurrentDirectory)!;
        var nonRoot = Path.Combine(root, "tmp", "x");

        Assert.True(DestinationPathGuard.IsRootPath(root));
        Assert.False(DestinationPathGuard.IsRootPath(nonRoot));
        Assert.False(DestinationPathGuard.IsRootPath(""));
    }

    [Fact]
    public void PrepareMaterializationTarget_RespectsOverwrite()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "sample.bin");
        File.WriteAllBytes(tempFile, new byte[] { 0x01 });

        try
        {
            Assert.False(DestinationPathGuard.PrepareMaterializationTarget(tempFile, overwrite: false, opt));
            Assert.True(DestinationPathGuard.PrepareMaterializationTarget(tempFile, overwrite: true, opt));
            Assert.False(File.Exists(tempFile));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateNewExtractionTarget_RejectsExistingAndMissingParent()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var existing = Path.Combine(tempDir, "exists.bin");
        File.WriteAllBytes(existing, new byte[] { 0x01 });

        try
        {
            Assert.False(DestinationPathGuard.ValidateNewExtractionTarget(existing, opt));
            Assert.False(DestinationPathGuard.ValidateNewExtractionTarget("no_parent.bin", opt));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ArchiveEntryPathPolicy_NormalizesAndRejectsInvalidPaths()
    {
        var normalized = string.Empty;
        var isDirectory = false;

        Assert.False(ArchiveEntryPathPolicy.TryNormalizeRelativePath(null, allowDirectoryMarker: false, ref normalized,
            ref isDirectory));
        Assert.False(ArchiveEntryPathPolicy.TryNormalizeRelativePath("a\0b", allowDirectoryMarker: false,
            ref normalized, ref isDirectory));
        Assert.False(ArchiveEntryPathPolicy.TryNormalizeRelativePath("/rooted.txt", allowDirectoryMarker: false,
            ref normalized, ref isDirectory));
        Assert.False(ArchiveEntryPathPolicy.TryNormalizeRelativePath("..", allowDirectoryMarker: false, ref normalized,
            ref isDirectory));
        Assert.False(ArchiveEntryPathPolicy.TryNormalizeRelativePath("a/../b", allowDirectoryMarker: false,
            ref normalized, ref isDirectory));
        Assert.False(ArchiveEntryPathPolicy.TryNormalizeRelativePath("a/", allowDirectoryMarker: false, ref normalized,
            ref isDirectory));

        Assert.True(ArchiveEntryPathPolicy.TryNormalizeRelativePath("a/", allowDirectoryMarker: true, ref normalized,
            ref isDirectory));
        Assert.Equal("a/", normalized);
        Assert.True(isDirectory);
    }

    [Fact]
    public void ArchiveSignaturePayloadGuard_DetectsZipMagic()
    {
        var zipMagic = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        var nonZip = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        Assert.True(ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(zipMagic));
        Assert.False(ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(nonZip));
    }

    [Fact]
    public void ArchivePayloadGuard_RejectsInvalidInputs()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();

        Assert.False(ArchivePayloadGuard.IsSafeArchivePayload(null, opt));
        Assert.False(ArchivePayloadGuard.IsSafeArchivePayload(Array.Empty<byte>(), opt));
        Assert.False(ArchivePayloadGuard.IsSafeArchivePayload(new byte[10], null));
    }

    [Fact]
    public void ArchivePayloadGuard_AllowsSmallValidZip()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var payload = CreateZipBytes(("a.txt", new byte[] { 0x01, 0x02 }));

        Assert.True(ArchivePayloadGuard.IsSafeArchivePayload(payload, opt));
    }

    private static byte[] CreateZipBytes(params (string path, byte[] content)[] entries)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (path, content) in entries)
            {
                var entry = zip.CreateEntry(path);
                if (content.Length == 0) continue;
                using var s = entry.Open();
                s.Write(content, 0, content.Length);
            }
        }

        return ms.ToArray();
    }
}
