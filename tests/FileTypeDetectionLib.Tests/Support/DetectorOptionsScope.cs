using System.Diagnostics.CodeAnalysis;
using FileTypeDetection;

namespace FileTypeDetectionLib.Tests.Support;

internal sealed class DetectorOptionsScope : IDisposable
{
    private readonly FileTypeProjectOptions _original = FileTypeDetector.GetDefaultOptions();

    public void Dispose()
    {
        FileTypeDetector.SetDefaultOptions(_original);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API keeps BDD/test call sites explicit and scoped.")]
    internal void Set(FileTypeProjectOptions options)
    {
        FileTypeDetector.SetDefaultOptions(options);
    }
}
