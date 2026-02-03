using System;
using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Support;

internal sealed class DetectorOptionsScope : IDisposable
{
    private readonly FileTypeDetectorOptions _original = FileTypeDetector.GetDefaultOptions();

    internal void Set(FileTypeDetectorOptions options)
    {
        FileTypeDetector.SetDefaultOptions(options);
    }

    public void Dispose()
    {
        FileTypeDetector.SetDefaultOptions(_original);
    }
}
