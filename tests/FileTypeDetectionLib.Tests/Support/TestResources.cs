using System;
using System.IO;

namespace FileTypeDetectionLib.Tests.Support;

internal static class TestResources
{
    private static readonly Lazy<FixtureManifestCatalog> Catalog = new(CreateCatalog);

    internal static string Resolve(string fixtureIdOrFileName)
    {
        return Catalog.Value.ResolvePath(fixtureIdOrFileName);
    }

    internal static FixtureManifestEntry Describe(string fixtureIdOrFileName)
    {
        return Catalog.Value.ResolveEntry(fixtureIdOrFileName);
    }

    private static FixtureManifestCatalog CreateCatalog()
    {
        var resourcesRoot = Path.Combine(AppContext.BaseDirectory, "resources");
        return FixtureManifestCatalog.LoadAndValidate(resourcesRoot);
    }
}