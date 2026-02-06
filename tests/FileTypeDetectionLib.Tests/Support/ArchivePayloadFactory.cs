using System;
using System.Formats.Tar;
using System.IO;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace FileTypeDetectionLib.Tests.Support;

internal static class ArchivePayloadFactory
{
    internal static byte[] CreateZipWithSingleEntry(string entryName, string content)
    {
        using var ms = new MemoryStream();
        using (var writer = WriterFactory.Open(ms, ArchiveType.Zip, new WriterOptions(CompressionType.Deflate)))
        using (var payload = new MemoryStream(Encoding.UTF8.GetBytes(content)))
        {
            writer.Write(string.IsNullOrWhiteSpace(entryName) ? "note.txt" : entryName, payload, DateTime.UnixEpoch);
        }

        return ms.ToArray();
    }

    internal static byte[] CreateTarWithSingleEntry(string entryName, string content)
    {
        using var ms = new MemoryStream();
        using (var writer = WriterFactory.Open(ms, ArchiveType.Tar, new WriterOptions(CompressionType.None)))
        using (var payload = new MemoryStream(Encoding.UTF8.GetBytes(content)))
        {
            writer.Write(string.IsNullOrWhiteSpace(entryName) ? "note.txt" : entryName, payload, DateTime.UnixEpoch);
        }

        return ms.ToArray();
    }

    internal static byte[] CreateGZipWithSingleEntry(string entryName, byte[] payload)
    {
        using var ms = new MemoryStream();
        using (var writer = WriterFactory.Open(ms, ArchiveType.GZip, new WriterOptions(CompressionType.GZip)))
        using (var source = new MemoryStream(payload, writable: false))
        {
            writer.Write(string.IsNullOrWhiteSpace(entryName) ? "payload.bin" : entryName, source, DateTime.UnixEpoch);
        }

        return ms.ToArray();
    }

    internal static byte[] CreateTarGzWithSingleEntry(string entryName, string content)
    {
        var tar = CreateTarWithSingleEntry(entryName, content);
        return CreateGZipWithSingleEntry("bundle.tar", tar);
    }

    internal static byte[] CreateTarWithSymlink(string fileName, string fileContent, string linkName, string linkTarget)
    {
        using var ms = new MemoryStream();
        using (var writer = new TarWriter(ms, leaveOpen: true))
        {
            var regular = new PaxTarEntry(TarEntryType.RegularFile, string.IsNullOrWhiteSpace(fileName) ? "note.txt" : fileName)
            {
                DataStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent))
            };
            writer.WriteEntry(regular);

            var symbolic = new PaxTarEntry(TarEntryType.SymbolicLink, string.IsNullOrWhiteSpace(linkName) ? "link.txt" : linkName)
            {
                LinkName = string.IsNullOrWhiteSpace(linkTarget) ? "note.txt" : linkTarget
            };
            writer.WriteEntry(symbolic);
        }

        return ms.ToArray();
    }
}
