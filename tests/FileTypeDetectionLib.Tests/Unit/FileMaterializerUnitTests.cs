using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileMaterializerUnitTests
{
    [Fact]
    public void Persist_WritesRawBytes_ByDefault()
    {
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "raw.bin");
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        try
        {
            var ok = FileMaterializer.Persist(payload, destination);

            Assert.True(ok);
            Assert.Equal(payload, File.ReadAllBytes(destination));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void Persist_UsesSecureExtract_ForZipPayload_WhenEnabled()
    {
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "unzipped");
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.zip"));

        try
        {
            var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: true);

            Assert.True(ok);
            Assert.True(File.Exists(Path.Combine(destination, "note.txt")));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void Persist_FailsClosed_ForUnsafeZip_WhenSecureExtractEnabled()
    {
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "unzipped");
        var traversalZip = ZipPayloadFactory.CreateZipWithSingleEntry("../evil.txt", 8);

        try
        {
            var ok = FileMaterializer.Persist(traversalZip, destination, overwrite: false, secureExtract: true);

            Assert.False(ok);
            Assert.False(Directory.Exists(destination));
            Assert.False(File.Exists(Path.Combine(tempRoot, "evil.txt")));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void Persist_RespectsOverwrite_ForRawByteWrites()
    {
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "raw.bin");

        try
        {
            File.WriteAllBytes(destination, new byte[] { 0x01 });

            var withoutOverwrite = FileMaterializer.Persist(new byte[] { 0x02 }, destination);
            var withOverwrite = FileMaterializer.Persist(new byte[] { 0x03 }, destination, overwrite: true);

            Assert.False(withoutOverwrite);
            Assert.True(withOverwrite);
            Assert.Equal(new byte[] { 0x03 }, File.ReadAllBytes(destination));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void Persist_AllowsOverloadWithOverwriteOnly()
    {
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "raw.bin");

        try
        {
            var ok = FileMaterializer.Persist(new byte[] { 0x10 }, destination, overwrite: false);

            Assert.True(ok);
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void Persist_WritesRawBytes_WhenSecureExtractEnabledButPayloadIsNotZip()
    {
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "raw.bin");
        var payload = new byte[] { 0x01, 0x23, 0x45, 0x67 };

        try
        {
            var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: true);

            Assert.True(ok);
            Assert.Equal(payload, File.ReadAllBytes(destination));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void Persist_FailsClosed_ForMalformedZipHeader_WhenSecureExtractEnabled()
    {
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "unzipped");
        var malformedZipLikePayload = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0xAA, 0xBB, 0xCC, 0xDD };

        try
        {
            var ok = FileMaterializer.Persist(malformedZipLikePayload, destination, overwrite: false, secureExtract: true);

            Assert.False(ok);
            Assert.False(Directory.Exists(destination));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "ftd-materialize-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CleanupTempRoot(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
