using FileTypeDetection;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class ArchiveDescriptorUnitTests
{
    [Fact]
    public void UnknownDescriptor_ReturnsUnknownContainer()
    {
        var descriptor = ArchiveDescriptor.UnknownDescriptor();

        Assert.Equal(FileKind.Unknown, descriptor.LogicalKind);
        Assert.Equal(ArchiveContainerType.Unknown, descriptor.ContainerType);
        Assert.Empty(descriptor.ContainerChain);
    }

    [Fact]
    public void ForContainerType_MapsUnknownToUnknownDescriptor()
    {
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Unknown);

        Assert.Equal(FileKind.Unknown, descriptor.LogicalKind);
        Assert.Equal(ArchiveContainerType.Unknown, descriptor.ContainerType);
        Assert.Empty(descriptor.ContainerChain);
    }

    [Fact]
    public void WithChain_ClonesInputArray()
    {
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);
        var chain = new[] { ArchiveContainerType.Zip, ArchiveContainerType.GZip };

        var updated = descriptor.WithChain(chain);
        chain[0] = ArchiveContainerType.Rar;

        Assert.Equal(ArchiveContainerType.Zip, updated.ContainerChain[0]);
        Assert.Equal(ArchiveContainerType.GZip, updated.ContainerChain[1]);
    }

    [Fact]
    public void WithChain_AllowsNullChain_AsEmpty()
    {
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);
        var updated = descriptor.WithChain(null!);

        Assert.NotNull(updated.ContainerChain);
        Assert.Empty(updated.ContainerChain);
    }
}