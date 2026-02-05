using System;
using System.Diagnostics;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Benchmarks;

public sealed class DetectionBenchmarkSmokeTests
{
    [Fact]
    [Trait("Category", "Benchmark")]
    public void Detect_Logs_HeaderOnly_Vs_ArchiveHeavy_Duration()
    {
        var detector = new FileTypeDetector();
        var pdf = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));
        var zip = File.ReadAllBytes(TestResources.Resolve("sample.zip"));

        const int iterations = 200;

        var pdfSw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            var result = detector.Detect(pdf);
            if (result.Kind != FileKind.Pdf)
            {
                throw new InvalidOperationException($"Unexpected kind for pdf benchmark run: {result.Kind}");
            }
        }
        pdfSw.Stop();

        var zipSw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            var result = detector.Detect(zip);
            if (result.Kind != FileKind.Zip)
            {
                throw new InvalidOperationException($"Unexpected kind for zip benchmark run: {result.Kind}");
            }
        }
        zipSw.Stop();

        // Dokumentationszweck: reproduzierbare Messpunkte ohne harte Timing-Schwelle.
        var benchmarkLine = $"benchmark_detect_ms: pdf={pdfSw.ElapsedMilliseconds}, zip={zipSw.ElapsedMilliseconds}, iterations={iterations}";
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "benchmark_detect_ms.txt"), benchmarkLine);

        Assert.True(pdfSw.ElapsedMilliseconds >= 0);
        Assert.True(zipSw.ElapsedMilliseconds >= 0);
    }
}
