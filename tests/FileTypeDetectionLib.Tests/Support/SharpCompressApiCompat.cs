using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;

namespace FileTypeDetectionLib.Tests.Support;

internal static class SharpCompressApiCompat
{
    internal static IArchive OpenArchive(Stream stream)
    {
        var options = new ReaderOptions { LeaveStreamOpen = true };
        var opened = InvokeOpen(typeof(ArchiveFactory), stream, options);
        return (IArchive)opened;
    }

    internal static IArchive OpenZipArchive(Stream stream)
    {
        var options = new ReaderOptions { LeaveStreamOpen = true };
        var opened = InvokeOpen(typeof(ZipArchive), stream, options);
        return (IArchive)opened;
    }

    internal static IWriter OpenWriter(Stream stream, ArchiveType archiveType, WriterOptions options)
    {
        var args = new object[] { stream, archiveType, options };
        var signature = new[] { typeof(Stream), typeof(ArchiveType), typeof(WriterOptions) };

        var method = typeof(WriterFactory).GetMethod("OpenWriter", signature)
            ?? typeof(WriterFactory).GetMethod("Open", signature)
            ?? throw new MissingMethodException(typeof(WriterFactory).FullName, "OpenWriter/Open(Stream, ArchiveType, WriterOptions) [compat]");

        return (IWriter)method.Invoke(null, args)!;
    }

    private static object InvokeOpen(Type type, Stream stream, ReaderOptions options)
    {
        var args = new object[] { stream, options };
        var signature = new[] { typeof(Stream), typeof(ReaderOptions) };

        var method = type.GetMethod("OpenArchive", signature)
            ?? type.GetMethod("Open", signature)
            ?? throw new MissingMethodException(type.FullName, "OpenArchive/Open(Stream, ReaderOptions)");

        return method.Invoke(null, args)!;
    }
}
