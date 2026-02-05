using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class FileMaterializerUnitTests
{
    [Fact]
    public void Persist_Fails_WhenPayloadExceedsConfiguredMaxBytes()
    {
        var original = FileTypeOptions.GetSnapshot();
        var options = FileTypeOptions.GetSnapshot();
        options.MaxBytes = 4;
        FileTypeOptions.SetSnapshot(options);

        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "raw.bin");
        var payload = new byte[] { 1, 2, 3, 4, 5 };

        try
        {
            var ok = FileMaterializer.Persist(payload, destination);
            Assert.False(ok);
            Assert.False(File.Exists(destination));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

    [Fact]
    public void Persist_WritesRawBytes_ByDefault()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "raw.bin");
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        var ok = FileMaterializer.Persist(payload, destination);

        Assert.True(ok);
        Assert.Equal(payload, File.ReadAllBytes(destination));
    }

    [Fact]
    public void Persist_UsesSecureExtract_ForZipPayload_WhenEnabled()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "unzipped");
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.zip"));

        var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: true);

        Assert.True(ok);
        Assert.True(File.Exists(Path.Combine(destination, "note.txt")));
    }

    [Fact]
    public void Persist_FailsClosed_ForUnsafeZip_WhenSecureExtractEnabled()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "unzipped");
        var traversalZip = ZipPayloadFactory.CreateZipWithSingleEntry("../evil.txt", 8);

        var ok = FileMaterializer.Persist(traversalZip, destination, overwrite: false, secureExtract: true);

        Assert.False(ok);
        Assert.False(Directory.Exists(destination));
        Assert.False(File.Exists(Path.Combine(tempRoot.RootPath, "evil.txt")));
    }

    [Fact]
    public void Persist_RespectsOverwrite_ForRawByteWrites()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "raw.bin");

        File.WriteAllBytes(destination, new byte[] { 0x01 });

        var withoutOverwrite = FileMaterializer.Persist(new byte[] { 0x02 }, destination);
        var withOverwrite = FileMaterializer.Persist(new byte[] { 0x03 }, destination, overwrite: true);

        Assert.False(withoutOverwrite);
        Assert.True(withOverwrite);
        Assert.Equal(new byte[] { 0x03 }, File.ReadAllBytes(destination));
    }

    [Fact]
    public void Persist_Fails_WithoutOverwrite_WhenDestinationAlreadyExists_AndPreservesOriginalBytes()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "conflict.bin");
        var original = new byte[] { 0xAA, 0xBB, 0xCC };

        File.WriteAllBytes(destination, original);
        var ok = FileMaterializer.Persist(new byte[] { 0x10, 0x20 }, destination, overwrite: false, secureExtract: false);

        Assert.False(ok);
        Assert.Equal(original, File.ReadAllBytes(destination));
    }

    [Fact]
    public void Persist_AllowsOverloadWithOverwriteOnly()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "raw.bin");

        var ok = FileMaterializer.Persist(new byte[] { 0x10 }, destination, overwrite: false);

        Assert.True(ok);
    }

    [Fact]
    public void Persist_WritesRawBytes_WhenSecureExtractEnabledButPayloadIsNotZip()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "raw.bin");
        var payload = new byte[] { 0x01, 0x23, 0x45, 0x67 };

        var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: true);

        Assert.True(ok);
        Assert.Equal(payload, File.ReadAllBytes(destination));
    }

    [Fact]
    public void Persist_FailsClosed_ForMalformedZipHeader_WhenSecureExtractEnabled()
    {
        using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
        var destination = Path.Combine(tempRoot.RootPath, "unzipped");
        var malformedZipLikePayload = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0xAA, 0xBB, 0xCC, 0xDD };

        var ok = FileMaterializer.Persist(malformedZipLikePayload, destination, overwrite: false, secureExtract: true);

        Assert.False(ok);
        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public void Persist_Fails_ForRootDestinationPath()
    {
        var payload = new byte[] { 0x41 };
        var rootPath = Path.GetPathRoot(Path.GetTempPath());
        Assert.False(string.IsNullOrWhiteSpace(rootPath));

        var ok = FileMaterializer.Persist(payload, rootPath!, overwrite: true, secureExtract: false);
        Assert.False(ok);
    }

    [Fact]
    public void Persist_Fails_ForWhitespaceDestinationPath()
    {
        var payload = new byte[] { 0x41 };
        var ok = FileMaterializer.Persist(payload, "   ", overwrite: false, secureExtract: false);
        Assert.False(ok);
    }

    [Fact]
    public void Persist_Fails_WhenPayloadExceedsConfiguredMaxBytes_WithPdfFixture()
    {
        var original = FileTypeOptions.GetSnapshot();
        var options = FileTypeOptions.GetSnapshot();
        options.MaxBytes = 16;
        FileTypeOptions.SetSnapshot(options);

        try
        {
            var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));
            using var tempRoot = TestTempPaths.CreateScope("ftd-materialize-test");
            var destination = Path.Combine(tempRoot.RootPath, "too-large.bin");

            var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: false);
            Assert.False(ok);
            Assert.False(File.Exists(destination));
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
        }
    }

}
