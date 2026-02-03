using System;
using System.IO;
using System.IO.Compression;

namespace FileTypeDetectionLib.Tests.Support;

internal static class ZipPayloadFactory
{
    internal static byte[] CreateZipWithEntries(int entryCount, int entrySize)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < entryCount; i++)
            {
                var entry = zip.CreateEntry($"entry_{i}.bin", CompressionLevel.SmallestSize);
                using var es = entry.Open();
                var payload = CreatePayload(entrySize, (byte)('A' + (i % 20)));
                es.Write(payload, 0, payload.Length);
            }
        }

        return ms.ToArray();
    }

    internal static byte[] CreateNestedZip(int nestedZipBytes)
    {
        return CreateNestedZipWithInnerLength(nestedZipBytes).zipBytes;
    }

    internal static (byte[] zipBytes, long innerUncompressedBytes) CreateNestedZipWithInnerLength(int nestedZipBytes)
    {
        var nestedContent = CreateZipWithEntries(1, Math.Max(1, nestedZipBytes));

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var nestedEntry = zip.CreateEntry("inner.zip", CompressionLevel.SmallestSize);
            using var entryStream = nestedEntry.Open();
            entryStream.Write(nestedContent, 0, nestedContent.Length);
        }

        return (ms.ToArray(), nestedContent.LongLength);
    }

    private static byte[] CreatePayload(int size, byte value)
    {
        var data = new byte[size];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = value;
        }

        return data;
    }
}
