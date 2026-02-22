namespace Tomtastisch.FileClassifier.CSCore.Model;

/// <summary>
/// <b>Zweck:</b><br/>
/// Unveränderliche Momentaufnahme für normalisierte projektweite Detektions- und Archivoptionen.
/// </summary>
public sealed record ProjectOptionsSnapshot
{
    public bool HeaderOnlyNonZip { get; init; } = true;
    public long MaxBytes { get; init; } = 200L * 1024L * 1024L;
    public int SniffBytes { get; init; } = 64 * 1024;
    public int MaxZipEntries { get; init; } = 5000;
    public long MaxZipTotalUncompressedBytes { get; init; } = 500L * 1024L * 1024L;
    public long MaxZipEntryUncompressedBytes { get; init; } = 200L * 1024L * 1024L;
    public int MaxZipCompressionRatio { get; init; } = 50;
    public int MaxZipNestingDepth { get; init; } = 2;
    public long MaxZipNestedBytes { get; init; } = 50L * 1024L * 1024L;
    public bool RejectArchiveLinks { get; init; } = true;
    public bool AllowUnknownArchiveEntrySize { get; init; }
    public HashOptionsSnapshot DeterministicHash { get; init; } = new();
}
