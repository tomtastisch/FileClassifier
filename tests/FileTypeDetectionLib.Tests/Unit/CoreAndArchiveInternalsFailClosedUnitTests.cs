using FileTypeDetection;
using FileTypeDetectionLib.Tests.Support;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;
using SharpCompress.Common;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class CoreAndArchiveInternalsFailClosedUnitTests
{
    [Fact]
    public void StreamBounds_CopyBounded_Throws_WhenLimitIsExceeded()
    {
        using var input = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        using var output = new MemoryStream();

        Assert.Throws<InvalidOperationException>(() => StreamBounds.CopyBounded(input, output, maxBytes: 4));
    }

    [Fact]
    public void StreamBounds_CopyBounded_CopiesAllBytes_WithinLimit()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        using var input = new MemoryStream(payload);
        using var output = new MemoryStream();

        StreamBounds.CopyBounded(input, output, maxBytes: payload.Length);

        Assert.Equal(payload, output.ToArray());
    }

    [Fact]
    public void ArchiveSignaturePayloadGuard_IsArchiveSignatureCandidate_DistinguishesArchiveAndNonArchive()
    {
        var archiveBytes = ArchivePayloadFactory.CreateZipWithSingleEntry("inner/note.txt", "hello");
        var nonArchiveBytes = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));

        Assert.True(ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(archiveBytes));
        Assert.False(ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(nonArchiveBytes));
        Assert.False(ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(Array.Empty<byte>()));
        Assert.False(ArchiveSignaturePayloadGuard.IsArchiveSignatureCandidate(null!));
    }

    [Fact]
    public void ArchiveBackendRegistry_ResolvesKnownBackends_AndRejectsUnknown()
    {
        Assert.IsType<ArchiveManagedBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.Zip));
        Assert.IsType<SharpCompressArchiveBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.Tar));
        Assert.IsType<SharpCompressArchiveBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.GZip));
        Assert.IsType<SharpCompressArchiveBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.SevenZip));
        Assert.IsType<SharpCompressArchiveBackend>(ArchiveBackendRegistry.Resolve(ArchiveContainerType.Rar));
        Assert.Null(ArchiveBackendRegistry.Resolve(ArchiveContainerType.Unknown));
    }

    [Theory]
    [InlineData("fx.sample_zip", (int)ArchiveContainerType.Zip)]
    [InlineData("fx.sample_7z", (int)ArchiveContainerType.SevenZip)]
    [InlineData("fx.sample_rar", (int)ArchiveContainerType.Rar)]
    public void ArchiveTypeResolver_TryDescribeBytes_MapsContainerTypeDeterministically(string fixtureId,
        int expectedTypeValue)
    {
        var options = FileTypeOptions.GetSnapshot();
        var payload = File.ReadAllBytes(TestResources.Resolve(fixtureId));
        ArchiveDescriptor descriptor = null!;

        var ok = ArchiveTypeResolver.TryDescribeBytes(payload, options, ref descriptor);

        Assert.True(ok);
        Assert.NotNull(descriptor);
        Assert.Equal((ArchiveContainerType)expectedTypeValue, descriptor.ContainerType);
        Assert.Equal(FileKind.Zip, descriptor.LogicalKind);
    }

    [Fact]
    public void ArchiveTypeResolver_TryDescribeBytes_FailsClosed_ForNonArchivePayload()
    {
        var options = FileTypeOptions.GetSnapshot();
        var payload = File.ReadAllBytes(TestResources.Resolve("sample.pdf"));
        ArchiveDescriptor descriptor = null!;

        var ok = ArchiveTypeResolver.TryDescribeBytes(payload, options, ref descriptor);

        Assert.False(ok);
        Assert.NotNull(descriptor);
        Assert.Equal(ArchiveContainerType.Unknown, descriptor.ContainerType);
    }

    [Fact]
    public void ArchiveTypeResolver_MapArchiveType_ReturnsUnknown_ForUnsupportedEnumValue()
    {
        var mapped = ArchiveTypeResolver.MapArchiveType((ArchiveType)999);
        Assert.Equal(ArchiveContainerType.Unknown, mapped);
    }

    [Fact]
    public void ArchiveSafetyGate_FailsClosed_ForInvalidInputs()
    {
        var options = FileTypeOptions.GetSnapshot();
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);

        Assert.False(ArchiveSafetyGate.IsArchiveSafeBytes(null!, options, descriptor));
        Assert.False(ArchiveSafetyGate.IsArchiveSafeBytes(Array.Empty<byte>(), options, descriptor));
        Assert.False(ArchiveSafetyGate.IsArchiveSafeBytes(new byte[] { 1 }, null!, descriptor));
        Assert.False(ArchiveSafetyGate.IsArchiveSafeBytes(new byte[] { 1 }, options,
            ArchiveDescriptor.UnknownDescriptor()));

        using var unreadable = new NonReadableMemoryStream(new byte[] { 1, 2, 3 });
        Assert.False(ArchiveSafetyGate.IsArchiveSafeStream(unreadable, options, descriptor, depth: 0));
    }

    [Fact]
    public void ArchiveProcessingEngine_FailsClosed_ForInvalidInputs_AndSucceedsForValidZipStream()
    {
        var options = FileTypeOptions.GetSnapshot();
        var descriptor = ArchiveDescriptor.ForContainerType(ArchiveContainerType.Zip);
        var payload = ArchivePayloadFactory.CreateZipWithSingleEntry("inner/note.txt", "hello");

        Assert.False(ArchiveProcessingEngine.ProcessArchiveStream(null!, options, 0, descriptor, null));
        using (var stream = new MemoryStream(payload, false))
        {
            Assert.False(ArchiveProcessingEngine.ProcessArchiveStream(stream, null!, 0, descriptor, null));
        }

        using (var stream = new MemoryStream(payload, false))
        {
            Assert.False(ArchiveProcessingEngine.ProcessArchiveStream(stream, options, 0,
                ArchiveDescriptor.UnknownDescriptor(), null));
        }

        using (var stream = new MemoryStream(payload, false))
        {
            Assert.True(ArchiveProcessingEngine.ValidateArchiveStream(stream, options, depth: 0, descriptor));
        }
    }

    [Fact]
    public void SharpCompressEntryModel_ReturnsSafeDefaults_WhenEntryIsNull()
    {
        var model = new SharpCompressEntryModel(null!);

        Assert.Equal(string.Empty, model.RelativePath);
        Assert.False(model.IsDirectory);
        Assert.Null(model.UncompressedSize);
        Assert.Null(model.CompressedSize);
        Assert.Equal(string.Empty, model.LinkTarget);
        Assert.Same(Stream.Null, model.OpenStream());
    }

    [Fact]
    public void SharpCompressEntryModel_MapsArchiveEntryFields_ForConcreteEntry()
    {
        var payload = ArchivePayloadFactory.CreateTarWithSingleEntry("inner/note.txt", "hello");
        using var ms = new MemoryStream(payload, false);
        using var archive = ArchiveFactory.Open(ms);
        var entry = archive.Entries.First(e => !e.IsDirectory);
        var model = new SharpCompressEntryModel(entry);

        Assert.Equal("inner/note.txt", model.RelativePath);
        Assert.False(model.IsDirectory);
        Assert.True(model.UncompressedSize.HasValue);
        Assert.True(model.UncompressedSize.Value >= 5);

        using var source = model.OpenStream();
        using var reader = new StreamReader(source);
        var content = reader.ReadToEnd();
        Assert.Equal("hello", content);
    }

    [Fact]
    public void LogGuard_SwallowsLoggerExceptions_AndNeverThrows()
    {
        var logger = new ThrowingLogger();
        var ex = Record.Exception(() =>
        {
            LogGuard.Debug(logger, "debug");
            LogGuard.Warn(logger, "warn");
            LogGuard.Error(logger, "error", new InvalidOperationException("boom"));
            LogGuard.Debug(null!, "debug");
            LogGuard.Warn(null!, "warn");
            LogGuard.Error(null!, "error", new InvalidOperationException("boom"));
        });

        Assert.Null(ex);
    }

    private sealed class NonReadableMemoryStream(byte[] payload) : MemoryStream(payload)
    {
        public override bool CanRead => false;
    }

    private sealed class ThrowingLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NoopScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            throw new InvalidOperationException("logger failure");
        }

        private sealed class NoopScope : IDisposable
        {
            internal static readonly NoopScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
