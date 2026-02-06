using System;
using System.Linq;
using System.Reflection;
using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class DeterministicHashingNormalizedEntryUnitTests
{
    [Fact]
    public void NormalizedEntry_Defaults_WhenConstructedWithNulls()
    {
        var type = typeof(DeterministicHashing).GetNestedTypes(BindingFlags.NonPublic)
            .First(t => t.Name == "NormalizedEntry");

        var ctor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .First();

        var instance = ctor.Invoke(new object?[] { null, null });
        var relativePath = (string)type.GetField("RelativePath", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
        var content = (byte[])type.GetField("Content", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;

        Assert.Equal(string.Empty, relativePath);
        Assert.NotNull(content);
        Assert.Empty(content);
    }
}
