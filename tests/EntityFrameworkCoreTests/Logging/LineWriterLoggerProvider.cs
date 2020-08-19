using Microsoft.Extensions.Logging;
using System;

namespace EntityFrameworkCoreTests.Logging
{
    /// <summary>
    /// Creates a logger that logs to the given <see cref="ILineWritable"/>.
    /// </summary>
    public class LineWriterLoggerProvider : ILoggerProvider
    {
        private readonly ILineWritable _lineWriter;
        private readonly LogLevel _logLevel;

        public LineWriterLoggerProvider(ILineWritable lineWriter, LogLevel logLevel = LogLevel.Information)
        {
            _lineWriter = lineWriter;
            _logLevel = logLevel;
        }

        public ILogger CreateLogger(string categoryName) => new LineWriterLogger(_lineWriter, _logLevel);

        public void Dispose() { }

        private class LineWriterLogger : ILogger
        {
            private readonly ILineWritable _lineWriter;
            private readonly LogLevel _logLevel;

            public LineWriterLogger(ILineWritable lineWriter, LogLevel logLevel)
            {
                _lineWriter = lineWriter;
                _logLevel = logLevel;
            }

            public IDisposable BeginScope<TState>(TState state) => null!;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _logLevel;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _lineWriter.WriteLine(formatter(state, exception));
            }
        }
    }
}
