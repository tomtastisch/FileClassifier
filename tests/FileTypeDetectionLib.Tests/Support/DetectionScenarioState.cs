using System.Collections.Generic;
using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Support;

internal sealed class DetectionScenarioState
{
    internal string? CurrentPath { get; set; }
    internal FileType? LastResult { get; set; }
    internal bool? ExtensionMatchResult { get; set; }
    internal byte[]? CurrentPayload { get; set; }
    internal IReadOnlyList<ZipExtractedEntry>? LastExtractedEntries { get; set; }
    internal string? TempRoot { get; set; }
    internal string? LastMaterializedPath { get; set; }
    internal bool? LastPersistResult { get; set; }
    internal byte[]? ExistingFileBytes { get; set; }
    internal string? ExistingFilePath { get; set; }
    internal FileTypeDetectorOptions OriginalOptions { get; } = FileTypeDetector.GetDefaultOptions();
}
