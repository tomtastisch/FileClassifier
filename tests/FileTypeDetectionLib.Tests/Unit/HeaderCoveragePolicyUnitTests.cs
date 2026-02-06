using System;
using System.Linq;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class HeaderCoveragePolicyUnitTests
{
    [Fact]
    public void HeaderCoverage_HasNoUnexpectedFallbackOnlyKinds()
    {
        var expected = Array.Empty<FileKind>();

        var actual = FileTypeRegistry.KindsWithoutDirectContentDetection()
            .OrderBy(x => (int)x)
            .ToArray();

        Assert.Equal(expected, actual);
    }
}