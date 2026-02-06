using FileTypeDetection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FileTypeDetectionLib.Tests.Unit;

public sealed class LogGuardUnitTests
{
    [Fact]
    public void LogGuard_SwallowsLoggerExceptions()
    {
        var logger = new ThrowingLogger();

        LogGuard.Debug(logger, "debug");
        LogGuard.Warn(logger, "warn");
        LogGuard.Error(logger, "error", new InvalidOperationException("boom"));
    }

    [Fact]
    public void LogGuard_Noops_WhenLoggerNull()
    {
        LogGuard.Debug(null, "debug");
        LogGuard.Warn(null, "warn");
        LogGuard.Error(null, "error", new InvalidOperationException("boom"));
    }

    private sealed class ThrowingLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return new Noop();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            throw new InvalidOperationException("logging failed");
        }

        private sealed class Noop : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
