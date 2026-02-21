using System.Collections.Concurrent;
using FileTypeDetectionLib.Tests.Support;
using Microsoft.Extensions.Logging;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class HeaderDetectionWarningUnitTests
{
    [Fact]
    public void Detect_DoesNotLogWarning_ForStructuredDocxDetection()
    {
        using var scope = new DetectorOptionsScope();
        var logger = new CollectingLogger();
        scope.Set(new FileTypeProjectOptions
        {
            Logger = logger
        });

        var source = TestResources.Resolve("sample.docx");
        var detected = new FileTypeDetector().Detect(source);

        Assert.Equal(FileKind.Doc, detected.Kind);
        Assert.DoesNotContain(logger.Messages,
            m => m.Contains("Keine direkte Content-Erkennung", StringComparison.Ordinal));
    }

    [Fact]
    public void Detect_DoesNotLogWarning_ForDirectPdfHeaderDetection()
    {
        using var scope = new DetectorOptionsScope();
        var logger = new CollectingLogger();
        scope.Set(new FileTypeProjectOptions
        {
            Logger = logger
        });

        var source = TestResources.Resolve("sample.pdf");
        var detected = new FileTypeDetector().Detect(source);

        Assert.Equal(FileKind.Pdf, detected.Kind);
        Assert.DoesNotContain(logger.Messages,
            m => m.Contains("Keine direkte Content-Erkennung", StringComparison.Ordinal));
    }

    private sealed class CollectingLogger : ILogger
    {
        internal readonly ConcurrentQueue<string> Messages = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Enqueue(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            internal static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}