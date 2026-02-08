using FileTypeDetection;

var payload = "%PDF-1.7\nportable-consumer\n"u8.ToArray();
var detector = new FileTypeDetector();
var detected = detector.Detect(payload);
if (detected.Kind != FileKind.Pdf)
{
    throw new InvalidOperationException($"Expected Pdf but got {detected.Kind}.");
}

var evidence = DeterministicHashing.HashBytes(payload, "portable.pdf");
if (!evidence.Digests.HasPhysicalHash || !evidence.Digests.HasLogicalHash)
{
    throw new InvalidOperationException("Expected both physical and logical hashes.");
}

var outputDir = Path.Combine(Path.GetTempPath(), "ftd-portable-consumer");
Directory.CreateDirectory(outputDir);
var outputPath = Path.Combine(outputDir, "portable.bin");
if (!FileMaterializer.Persist(payload, outputPath, overwrite: true, secureExtract: false))
{
    throw new InvalidOperationException("Persist returned false.");
}

Console.WriteLine("Portable consumer smoke passed.");
