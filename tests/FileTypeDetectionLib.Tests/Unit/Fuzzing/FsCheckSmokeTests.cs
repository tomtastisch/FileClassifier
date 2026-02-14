using System.Linq;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit.Fuzzing;

public sealed class FsCheckSmokeTests
{
    [Fact]
    public void FuzzingSmoke_GeneratorProducesSamples()
    {
        // FsCheck-based fuzzing signal: randomized sample generation.
        var samples = FsCheck.FSharp.Gen.Sample(20, FsCheck.FSharp.Gen.Choose(0, 100));
        Assert.Equal(20, samples.Count());
        Assert.All(samples, value => Assert.InRange(value, 0, 100));
    }
}
