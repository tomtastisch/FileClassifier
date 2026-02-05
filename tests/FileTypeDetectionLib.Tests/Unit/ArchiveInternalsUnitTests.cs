using System;
using System.IO;
using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using SharpCompress.Common;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveInternalsUnitTests
{
    [Fact]
    public void ArchiveBackendRegistry_ResolvesManagedAndSharpCompressBackends()
    {
        Assert.IsType<ArchiveManagedBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.Zip));
        Assert.IsType<SharpCompressArchiveBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.Tar));
        Assert.IsType<SharpCompressArchiveBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.GZip));
        Assert.IsType<SharpCompressArchiveBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.SevenZip));
        Assert.IsType<SharpCompressArchiveBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.Rar));
        Assert.Null(ArchiveBackendRegistry.Resolve(ArchiveContainerType.Unknown));
    }

    [Fact]
    public void ArchiveTypeResolver_MapsArchiveTypes_ToContainerTypes()
    {
        Assert.Equal(ArchiveContainerType.Zip, ArchiveTypeResolver.MapArchiveType(ArchiveType.Zip));
        Assert.Equal(ArchiveContainerType.Tar, ArchiveTypeResolver.MapArchiveType(ArchiveType.Tar));
        Assert.Equal(ArchiveContainerType.GZip, ArchiveTypeResolver.MapArchiveType(ArchiveType.GZip));
        Assert.Equal(ArchiveContainerType.SevenZip, ArchiveTypeResolver.MapArchiveType(ArchiveType.SevenZip));
        Assert.Equal(ArchiveContainerType.Rar, ArchiveTypeResolver.MapArchiveType(ArchiveType.Rar));
        Assert.NotEqual(ArchiveContainerType.Zip, ArchiveTypeResolver.MapArchiveType((ArchiveType)0));
    }

    [Fact]
    public void ArchiveTypeResolver_TryDescribeBytes_DetectsArchives()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        ArchiveDescriptor descriptor = ArchiveDescriptor.UnknownDescriptor();

        Assert.False(ArchiveTypeResolver.TryDescribeBytes(Array.Empty<byte>(), opt, ref descriptor));

        var tar = ArchivePayloadFactory.CreateTarWithSingleEntry("note.txt", "hello");
        Assert.True(ArchiveTypeResolver.TryDescribeBytes(tar, opt, ref descriptor));
        Assert.Equal(ArchiveContainerType.Tar, descriptor.ContainerType);

        var gz = ArchivePayloadFactory.CreateTarGzWithSingleEntry("note.txt", "hello");
        Assert.True(ArchiveTypeResolver.TryDescribeBytes(gz, opt, ref descriptor));
        Assert.Equal(ArchiveContainerType.GZip, descriptor.ContainerType);
    }

    [Fact]
    public void ArchiveProcessingEngine_FailsClosed_ForInvalidInputs()
    {
        var opt = FileTypeProjectOptions.DefaultOptions();
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        Assert.False(ArchiveProcessingEngine.ProcessArchiveStream(null, opt, depth: 0, descriptor, extractEntry: null));
        Assert.False(ArchiveProcessingEngine.ProcessArchiveStream(new MemoryStream(), null, depth: 0, descriptor, extractEntry: null));
        Assert.False(ArchiveProcessingEngine.ProcessArchiveStream(new MemoryStream(), opt, depth: 0, descriptor, extractEntry: null));
    }

    [Fact]
    public void SharpCompressEntryModel_ReturnsSafeDefaults_ForNullEntry()
    {
        var model = new SharpCompressEntryModel(null);

        Assert.Equal(string.Empty, model.RelativePath);
        Assert.False(model.IsDirectory);
        Assert.Null(model.UncompressedSize);
        Assert.Null(model.CompressedSize);
        Assert.Equal(string.Empty, model.LinkTarget);
        Assert.Equal(Stream.Null, model.OpenStream());
    }

    [Fact]
    public void ArchiveManagedEntryModel_ReturnsSafeDefaults_ForNullEntry()
    {
        var model = new ArchiveManagedEntryModel(null);

        Assert.Equal(string.Empty, model.RelativePath);
        Assert.False(model.IsDirectory);
        Assert.Null(model.UncompressedSize);
        Assert.Null(model.CompressedSize);
        Assert.Equal(string.Empty, model.LinkTarget);
        Assert.Equal(Stream.Null, model.OpenStream());
    }
}
