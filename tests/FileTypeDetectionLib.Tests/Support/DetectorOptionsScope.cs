using System;
using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Support;

internal sealed class DetectorOptionsScope : IDisposable
{
    private readonly FileTypeProjectOptions _original = FileTypeDetector.GetDefaultOptions();

    public void Dispose()
    {
        FileTypeDetector.SetDefaultOptions(_original);
    }

    internal void Set(FileTypeProjectOptions options)
    {
        FileTypeDetector.SetDefaultOptions(options);
    }
}