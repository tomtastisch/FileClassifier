using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class CoreInternalsBranchUnitTests
{
    [Fact]
    public void ArchiveSafetyGate_ReturnsFalse_ForNullsAndUnknownDescriptor()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        Assert.False(ArchiveSafetyGate.IsArchiveSafeBytes(null, opt, descriptor));
        Assert.False(ArchiveSafetyGate.IsArchiveSafeBytes(Array.Empty<byte>(), opt, descriptor));
        Assert.False(ArchiveSafetyGate.IsArchiveSafeBytes(new byte[4], null, descriptor));
    }

    [Fact]
    public void ArchiveSafetyGate_ReturnsFalse_ForNullOptions_Stream()
    {
        var descriptor = ArchiveDescriptor.UnknownDescriptor();
        using var stream = new MemoryStream(new byte[] { 0x01 });

        Assert.False(ArchiveSafetyGate.IsArchiveSafeStream(stream, null!, descriptor, depth: 0));
    }

    [Fact]
    public void ArchivePayloadGuard_Rejects_WhenPayloadTooLarge()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        opt.MaxBytes = 4;

        var data = new byte[10];
        Assert.False(ArchivePayloadGuard.IsSafeArchivePayload(data, opt));
    }

    [Fact]
    public void DestinationPathGuard_RejectsRootTarget()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var root = Path.GetPathRoot(Environment.CurrentDirectory)!;

        Assert.False(DestinationPathGuard.PrepareMaterializationTarget(root, overwrite: false, opt));
        Assert.False(DestinationPathGuard.ValidateNewExtractionTarget(root, opt));
    }

    [Fact]
    public void ArchiveEntryPathPolicy_NormalizesBackslashes_AndRejectsRooted()
    {
        var normalized = string.Empty;
        var isDir = false;

        Assert.True(ArchiveEntryPathPolicy.TryNormalizeRelativePath("a\\b.txt", allowDirectoryMarker: false, ref normalized, ref isDir));
        Assert.Equal("a/b.txt", normalized);

        Assert.False(ArchiveEntryPathPolicy.TryNormalizeRelativePath("a//b.txt", allowDirectoryMarker: false, ref normalized, ref isDir));
    }
}
