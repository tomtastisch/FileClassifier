using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ZipExtractionUnitTests
{
    [Fact]
    public void ExtractZipSafe_Succeeds_ForValidZip_WithVerification()
    {
        var source = TestResources.Resolve("sample.zip");
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "out");

        try
        {
            var ok = new FileTypeDetector().ExtractZipSafe(source, destination, verifyBeforeExtract: true);

            Assert.True(ok);
            Assert.True(File.Exists(Path.Combine(destination, "note.txt")));
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void ExtractZipSafe_FailsClosed_ForTraversalEntry()
    {
        var tempRoot = CreateTempRoot();
        var zipPath = Path.Combine(tempRoot, "traversal.zip");
        var destination = Path.Combine(tempRoot, "out");
        File.WriteAllBytes(zipPath, ZipPayloadFactory.CreateZipWithSingleEntry("../evil.txt", 8));

        try
        {
            var ok = new FileTypeDetector().ExtractZipSafe(zipPath, destination, verifyBeforeExtract: false);

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
    public void ExtractZipSafe_Fails_WhenDestinationAlreadyExists()
    {
        var source = TestResources.Resolve("sample.zip");
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "out");
        Directory.CreateDirectory(destination);

        try
        {
            var ok = new FileTypeDetector().ExtractZipSafe(source, destination, verifyBeforeExtract: false);

            Assert.False(ok);
        }
        finally
        {
            CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void ExtractZipSafe_Fails_PreVerification_ForNonZipInput()
    {
        var source = TestResources.Resolve("sample.pdf");
        var tempRoot = CreateTempRoot();
        var destination = Path.Combine(tempRoot, "out");

        try
        {
            var ok = new FileTypeDetector().ExtractZipSafe(source, destination, verifyBeforeExtract: true);

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
        var path = Path.Combine(Path.GetTempPath(), "ftd-extract-test-" + Guid.NewGuid().ToString("N"));
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
