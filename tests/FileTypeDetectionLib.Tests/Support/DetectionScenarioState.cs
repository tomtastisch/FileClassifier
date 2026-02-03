using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Support;

internal sealed class DetectionScenarioState
{
    internal string? CurrentPath { get; set; }
    internal FileType? LastResult { get; set; }
    internal bool? ExtensionMatchResult { get; set; }
    internal FileTypeDetectorOptions OriginalOptions { get; } = FileTypeDetector.GetDefaultOptions();
}
