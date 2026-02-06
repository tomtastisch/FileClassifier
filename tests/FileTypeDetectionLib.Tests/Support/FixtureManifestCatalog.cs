using System.Security.Cryptography;
using System.Text.Json;

namespace FileTypeDetectionLib.Tests.Support;

internal sealed class FixtureManifestCatalog
{
    private const string ManifestFileName = "fixtures.manifest.json";
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly Dictionary<string, FixtureManifestEntry> _byFileName;
    private readonly Dictionary<string, FixtureManifestEntry> _byFixtureId;

    private readonly string _resourcesRoot;

    private FixtureManifestCatalog(
        string resourcesRoot,
        Dictionary<string, FixtureManifestEntry> byFixtureId,
        Dictionary<string, FixtureManifestEntry> byFileName)
    {
        _resourcesRoot = resourcesRoot;
        _byFixtureId = byFixtureId;
        _byFileName = byFileName;
    }

    internal IReadOnlyCollection<FixtureManifestEntry> Entries => _byFixtureId.Values.ToList().AsReadOnly();

    internal static FixtureManifestCatalog LoadAndValidate(string resourcesRoot)
    {
        if (string.IsNullOrWhiteSpace(resourcesRoot))
            throw new InvalidOperationException("Fixture resource root is empty.");

        var manifestPath = Path.Combine(resourcesRoot, ManifestFileName);
        if (!File.Exists(manifestPath)) throw new FileNotFoundException($"Fixture manifest missing: {manifestPath}");

        var doc = JsonSerializer.Deserialize<FixtureManifestDocument>(
            File.ReadAllText(manifestPath),
            JsonOptions);
        if (doc is null || doc.Fixtures is null || doc.Fixtures.Count == 0)
            throw new InvalidOperationException("Fixture manifest is empty.");

        var byFixtureId = new Dictionary<string, FixtureManifestEntry>(KeyComparer);
        var byFileName = new Dictionary<string, FixtureManifestEntry>(KeyComparer);

        foreach (var entry in doc.Fixtures)
        {
            ValidateEntryFields(entry);
            if (!byFixtureId.TryAdd(entry.FixtureId, entry))
                throw new InvalidOperationException($"Duplicate fixtureId in manifest: {entry.FixtureId}");

            if (!byFileName.TryAdd(entry.FileName, entry))
                throw new InvalidOperationException($"Duplicate fileName in manifest: {entry.FileName}");

            var filePath = Path.Combine(resourcesRoot, entry.FileName);
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Fixture file missing: {entry.FileName}");

            var actualSha = ComputeSha256(filePath);
            if (!string.Equals(actualSha, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Fixture hash mismatch for '{entry.FileName}'. expected={entry.Sha256}, actual={actualSha}");
        }

        ValidateCoverage(resourcesRoot, byFileName);
        return new FixtureManifestCatalog(resourcesRoot, byFixtureId, byFileName);
    }

    internal string ResolvePath(string nameOrFixtureId)
    {
        var entry = ResolveEntry(nameOrFixtureId);
        return Path.Combine(_resourcesRoot, entry.FileName);
    }

    internal FixtureManifestEntry ResolveEntry(string nameOrFixtureId)
    {
        if (string.IsNullOrWhiteSpace(nameOrFixtureId))
            throw new InvalidOperationException("Fixture lookup key is empty.");

        if (_byFixtureId.TryGetValue(nameOrFixtureId, out var byId)) return byId;

        if (_byFileName.TryGetValue(nameOrFixtureId, out var byName)) return byName;

        throw new FileNotFoundException($"Fixture not found by id or file name: {nameOrFixtureId}");
    }

    private static void ValidateCoverage(string resourcesRoot, Dictionary<string, FixtureManifestEntry> byFileName)
    {
        var allFiles = Directory.EnumerateFiles(resourcesRoot, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Where(name => !string.Equals(name, ManifestFileName, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(KeyComparer);

        foreach (var fileName in allFiles.Where(file => !byFileName.ContainsKey(file)))
            throw new InvalidOperationException($"Unlisted fixture file in resources directory: {fileName}");

        foreach (var listed in byFileName.Keys.Where(file => !allFiles.Contains(file)))
            throw new InvalidOperationException($"Manifest lists missing fixture file: {listed}");
    }

    private static void ValidateEntryFields(FixtureManifestEntry entry)
    {
        if (entry is null) throw new InvalidOperationException("Fixture manifest contains null entry.");

        if (string.IsNullOrWhiteSpace(entry.FixtureId))
            throw new InvalidOperationException("Fixture entry requires fixtureId.");

        if (string.IsNullOrWhiteSpace(entry.FileName))
            throw new InvalidOperationException($"Fixture '{entry.FixtureId}' requires fileName.");

        if (string.IsNullOrWhiteSpace(entry.DataType))
            throw new InvalidOperationException($"Fixture '{entry.FixtureId}' requires dataType.");

        if (string.IsNullOrWhiteSpace(entry.ObjectId))
            throw new InvalidOperationException($"Fixture '{entry.FixtureId}' requires objectId.");

        if (string.IsNullOrWhiteSpace(entry.Sha256) || entry.Sha256.Length != 64)
            throw new InvalidOperationException($"Fixture '{entry.FixtureId}' requires SHA-256 (64 hex chars).");
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

internal sealed class FixtureManifestDocument
{
    public int Version { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<FixtureManifestEntry> Fixtures { get; set; } = new();
}

internal sealed class FixtureManifestEntry
{
    public string FixtureId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceRef { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string SecurityNotes { get; set; } = string.Empty;
}
