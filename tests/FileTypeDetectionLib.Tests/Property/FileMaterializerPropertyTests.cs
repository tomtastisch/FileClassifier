using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
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
        var tempRoot = TestTempPaths.CreateTempRoot("ftd-materializer-prop");

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
            TestTempPaths.CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void Persist_RespectsOverwriteInvariant_ForDeterministicDestinationStates()
    {
        var original = FileTypeOptions.GetSnapshot();
        var options = FileTypeOptions.GetSnapshot();
        options.MaxBytes = 1024;
        FileTypeOptions.SetSnapshot(options);

        var rng = new Random(20260205);
        var tempRoot = TestTempPaths.CreateTempRoot("ftd-materializer-prop");

        try
        {
            for (var i = 0; i < 80; i++)
            {
                var destination = Path.Combine(tempRoot, $"overwrite-{i}.bin");
                var payload = new byte[rng.Next(1, 64)];
                rng.NextBytes(payload);

                var state = rng.Next(0, 3); // 0:none, 1:file, 2:dir
                var overwrite = rng.Next(0, 2) == 1;

                if (state == 1)
                {
                    File.WriteAllBytes(destination, new byte[] { 0xAA, 0xBB });
                }
                else if (state == 2)
                {
                    Directory.CreateDirectory(destination);
                    File.WriteAllBytes(Path.Combine(destination, "marker.txt"), new byte[] { 0x01 });
                }

                var ok = FileMaterializer.Persist(payload, destination, overwrite, secureExtract: false);
                var expected = (state == 0) || overwrite;

                Assert.Equal(expected, ok);

                if (expected)
                {
                    Assert.True(File.Exists(destination));
                    Assert.Equal(payload, File.ReadAllBytes(destination));
                    Assert.False(Directory.Exists(destination));
                }
                else
                {
                    if (state == 1)
                    {
                        Assert.True(File.Exists(destination));
                        Assert.Equal(new byte[] { 0xAA, 0xBB }, File.ReadAllBytes(destination));
                    }
                    else if (state == 2)
                    {
                        Assert.True(Directory.Exists(destination));
                        Assert.True(File.Exists(Path.Combine(destination, "marker.txt")));
                    }
                }
            }
        }
        finally
        {
            FileTypeOptions.SetSnapshot(original);
            TestTempPaths.CleanupTempRoot(tempRoot);
        }
    }

    [Fact]
    public void Persist_RejectsInvalidDestinations_WithoutSideEffects()
    {
        var payload = new byte[] { 0x10, 0x20 };
        var invalidDestinations = new[] { "", "   ", "\t", "\n" };

        foreach (var destination in invalidDestinations)
        {
            var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: false);
            Assert.False(ok);
        }
    }

}
