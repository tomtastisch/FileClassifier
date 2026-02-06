using System;
using System.IO;
using System.Threading;

namespace FileTypeDetectionLib.Tests.Support;

internal static class TestTempPaths
{
    internal static TempRootScope CreateScope(string prefix)
    {
        return new TempRootScope(CreateTempRoot(prefix));
    }

    internal static string CreateTempRoot(string prefix)
    {
        var safePrefix = string.IsNullOrWhiteSpace(prefix) ? "ftd-test" : prefix.Trim();
        var path = Path.Combine(Path.GetTempPath(), safePrefix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    internal static void CleanupTempRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

        // Retry briefly to absorb transient file handle timing on CI/macOS.
        for (var i = 0; i < 3; i++)
            try
            {
                Directory.Delete(path, true);
                return;
            }
            catch (IOException) when (i < 2)
            {
                Thread.Sleep(20);
            }
            catch (UnauthorizedAccessException) when (i < 2)
            {
                Thread.Sleep(20);
            }
    }

    internal sealed class TempRootScope : IDisposable
    {
        internal TempRootScope(string rootPath)
        {
            RootPath = rootPath;
        }

        internal string RootPath { get; }

        public void Dispose()
        {
            CleanupTempRoot(RootPath);
        }
    }
}