using FileTypeDetectionLib.Tests.Support;
using Tomtastisch.FileClassifier;

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

                switch (state)
                {
                    case 1:
                        File.WriteAllBytes(destination, new byte[] { 0xAA, 0xBB });
                        break;
                    case 2:
                        Directory.CreateDirectory(destination);
                        File.WriteAllBytes(Path.Combine(destination, "marker.txt"), new byte[] { 0x01 });
                        break;
                }

                var ok = FileMaterializer.Persist(payload, destination, overwrite, false);
                var expected = state == 0 || overwrite;

                Assert.Equal(expected, ok);

                if (expected)
                {
                    Assert.True(File.Exists(destination));
                    Assert.Equal(payload, File.ReadAllBytes(destination));
                    Assert.False(Directory.Exists(destination));
                }
                else
                {
                    switch (state)
                    {
                        case 1:
                            Assert.True(File.Exists(destination));
                            Assert.Equal(new byte[] { 0xAA, 0xBB }, File.ReadAllBytes(destination));
                            break;
                        case 2:
                            Assert.True(Directory.Exists(destination));
                            Assert.True(File.Exists(Path.Combine(destination, "marker.txt")));
                            break;
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
            var ok = FileMaterializer.Persist(payload, destination, false, false);
            Assert.False(ok);
        }
    }

    [Fact]
    public void Persist_ReturnsFalse_ForDestinationWithNullChar()
    {
        var payload = new byte[] { 0x10, 0x20 };
        const string destination = "out\0bad.bin";

        var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: false);

        Assert.False(ok);
    }

    [Fact]
    public void Persist_SecureExtractDecisionMatrix_IsDeterministic_ForPayloadKinds()
    {
        var random = new Random(20260222);
        var validZip = ArchivePayloadFactory.CreateZipWithSingleEntry("inner/note.txt", "matrix");
        var malformedArchiveSignature = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0xAA, 0xBB, 0xCC, 0xDD };
        var plainPayload = new byte[] { 0x01, 0x23, 0x45, 0x67 };

        using var tempScope = TestTempPaths.CreateScope("ftd-materializer-matrix");

        for (var i = 0; i < 60; i++)
        {
            var secureExtract = random.Next(0, 2) == 1;
            var payloadKind = random.Next(0, 3); // 0=plain, 1=valid-zip, 2=malformed-signature

            var payload = payloadKind switch
            {
                1 => validZip,
                2 => malformedArchiveSignature,
                _ => plainPayload
            };

            var destination = secureExtract && payloadKind == 1
                ? Path.Combine(tempScope.RootPath, $"extract-{i}")
                : Path.Combine(tempScope.RootPath, $"persist-{i}.bin");

            var ok = FileMaterializer.Persist(payload, destination, overwrite: false, secureExtract: secureExtract);
            var expected = !(secureExtract && payloadKind == 2);

            Assert.Equal(expected, ok);

            if (expected && secureExtract && payloadKind == 1)
            {
                Assert.True(File.Exists(Path.Combine(destination, "inner", "note.txt")));
            }

            if (expected && (!secureExtract || payloadKind != 1))
            {
                Assert.True(File.Exists(destination));
            }

            if (!expected)
            {
                Assert.False(File.Exists(destination));
            }
        }
    }
}
