using System;
using System.IO;

namespace FileTypeDetectionLib.Tests.Support;

internal static class TestResources
{
    internal static string Resolve(string name) =>
        Path.Combine(AppContext.BaseDirectory, "resources", name);
}
