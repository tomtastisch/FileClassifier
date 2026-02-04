using System;
using System.IO;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Property;

public sealed class FileMaterializerPropertyTests
{
    [Fact]
    public void Persist_RespectsMaxBytes_ForDeterministicRandomPayloadLengths()
    {
        var original = FileTypeOptions.GetSnapshot();
        var options = FileTypeOptions.GetSnapshot();
        options.MaxBytes = 128;
        FileTypeOptions.SetSnapshot(options);

        var rng = new Random(20260204);
        var tempRoot = CreateTempRoot();

        try
        {
            for (var i = 0; i < 80; i++)
            {
                var length = rng.Next(0, 257);
                var payload = new byte[length];
                rng.NextBytes(payload);

                var destination = Path.Combine(tempRoot, $"payload-{i}.bin");
                var ok = FileMaterializer.Persist(payload, destination);
                var expected = length <= options.MaxBytes;

                Assert.Equal(expected, ok);
                if (expected)
                {
                    Assert.True(File.Exists(destination));
                    Assert.Equal(length, new FileInfo(destination).Length);
                }
                else
                {
                    Assert.False(File.Exists(destination));
                }
            }
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
            CleanupTempRoot(tempRoot);
        }
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "ftd-materializer-prop-" + Guid.NewGuid().ToString("N"));
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
