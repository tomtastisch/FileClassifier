using Tomtastisch.FileClassifier.CSCore.Mapping;
using Tomtastisch.FileClassifier.CSCore.Model;

namespace Tomtastisch.FileClassifier.CSCore.Utilities;

/// <summary>
/// <b>Zweck:</b><br/>
/// Normalisierungs- und Projektionshilfen für Projektoptions-Momentaufnahmen.
/// </summary>
public static class ProjectOptionsUtility
{
    public const long MinPositiveLong = 1;
    public const int MinPositiveInt = 1;
    public const int MinNonNegativeInt = 0;

    public const int ValueIndexMaxBytes = 0;
    public const int ValueIndexSniffBytes = 1;
    public const int ValueIndexMaxZipEntries = 2;
    public const int ValueIndexMaxZipTotalUncompressedBytes = 3;
    public const int ValueIndexMaxZipEntryUncompressedBytes = 4;
    public const int ValueIndexMaxZipCompressionRatio = 5;
    public const int ValueIndexMaxZipNestingDepth = 6;
    public const int ValueIndexMaxZipNestedBytes = 7;
    public const int ValueIndexRejectArchiveLinks = 8;
    public const int ValueIndexAllowUnknownArchiveEntrySize = 9;
    public const int ValueIndexHashIncludePayloadCopies = 10;
    public const int ValueIndexHashIncludeFastHash = 11;
    public const int ValueIndexHashIncludeSecureHash = 12;
    public const int ValueIndexHashMaterializedFileName = 13;
    public const int NormalizedValueCount = 14;

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Normalisiert primitive Optionswerte und liefert sie als deterministischen Wertevektor zurück.
    /// </summary>
    public static object[] NormalizeSnapshotValues(
        bool headerOnlyNonZip,
        long maxBytes,
        int sniffBytes,
        int maxZipEntries,
        long maxZipTotalUncompressedBytes,
        long maxZipEntryUncompressedBytes,
        int maxZipCompressionRatio,
        int maxZipNestingDepth,
        long maxZipNestedBytes,
        bool rejectArchiveLinks,
        bool allowUnknownArchiveEntrySize,
        bool hashIncludePayloadCopies,
        bool hashIncludeFastHash,
        bool hashIncludeSecureHash,
        string? hashMaterializedFileName)
    {
        var source = new ProjectOptionsSnapshot
        {
            HeaderOnlyNonZip = headerOnlyNonZip,
            MaxBytes = maxBytes,
            SniffBytes = sniffBytes,
            MaxZipEntries = maxZipEntries,
            MaxZipTotalUncompressedBytes = maxZipTotalUncompressedBytes,
            MaxZipEntryUncompressedBytes = maxZipEntryUncompressedBytes,
            MaxZipCompressionRatio = maxZipCompressionRatio,
            MaxZipNestingDepth = maxZipNestingDepth,
            MaxZipNestedBytes = maxZipNestedBytes,
            RejectArchiveLinks = rejectArchiveLinks,
            AllowUnknownArchiveEntrySize = allowUnknownArchiveEntrySize,
            DeterministicHash = new HashOptionsSnapshot
            {
                IncludePayloadCopies = hashIncludePayloadCopies,
                IncludeFastHash = hashIncludeFastHash,
                IncludeSecureHash = hashIncludeSecureHash,
                MaterializedFileName = hashMaterializedFileName ?? string.Empty
            }
        };

        var normalized = Normalize(source);
        var normalizedHash = normalized.DeterministicHash;

        return
        [
            normalized.MaxBytes,
            normalized.SniffBytes,
            normalized.MaxZipEntries,
            normalized.MaxZipTotalUncompressedBytes,
            normalized.MaxZipEntryUncompressedBytes,
            normalized.MaxZipCompressionRatio,
            normalized.MaxZipNestingDepth,
            normalized.MaxZipNestedBytes,
            normalized.RejectArchiveLinks,
            normalized.AllowUnknownArchiveEntrySize,
            normalizedHash.IncludePayloadCopies,
            normalizedHash.IncludeFastHash,
            normalizedHash.IncludeSecureHash,
            normalizedHash.MaterializedFileName
        ];
    }

    /// <summary>
    /// <b>Verhalten:</b><br/>
    /// Erzeugt einen normalisierten Klon der übergebenen Options-Momentaufnahme.
    /// </summary>
    public static ProjectOptionsSnapshot Normalize(ProjectOptionsSnapshot? source)
    {
        var effectiveSource = source ?? new ProjectOptionsSnapshot();
        var cloned = ProjectOptionsSnapshotMapper.Clone(effectiveSource);
        var clonedHash = cloned.DeterministicHash is null
            ? new HashOptionsSnapshot()
            : ProjectOptionsSnapshotMapper.Clone(cloned.DeterministicHash);

        var normalizedHash = clonedHash with
        {
            MaterializedFileName = HashNormalizationUtility.NormalizeMaterializedFileName(
                clonedHash.MaterializedFileName)
        };

        return cloned with
        {
            MaxBytes = Math.Max(MinPositiveLong, cloned.MaxBytes),
            SniffBytes = Math.Max(MinPositiveInt, cloned.SniffBytes),
            MaxZipEntries = Math.Max(MinPositiveInt, cloned.MaxZipEntries),
            MaxZipTotalUncompressedBytes = Math.Max(MinPositiveLong, cloned.MaxZipTotalUncompressedBytes),
            MaxZipEntryUncompressedBytes = Math.Max(MinPositiveLong, cloned.MaxZipEntryUncompressedBytes),
            MaxZipCompressionRatio = Math.Max(MinNonNegativeInt, cloned.MaxZipCompressionRatio),
            MaxZipNestingDepth = Math.Max(MinNonNegativeInt, cloned.MaxZipNestingDepth),
            MaxZipNestedBytes = Math.Max(MinPositiveLong, cloned.MaxZipNestedBytes),
            DeterministicHash = normalizedHash
        };
    }
}
