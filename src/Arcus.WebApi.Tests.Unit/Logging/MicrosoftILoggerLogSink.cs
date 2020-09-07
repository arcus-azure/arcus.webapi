using System;
using GuardNet;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    /// <summary>
    /// Represents an <see cref="ILogEventSink"/> implementation to write log messages to an Microsoft <see cref="ILogger"/> logger.
    /// </summary>
    public class MicrosoftILoggerLogSink : ILogEventSink
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftILoggerLogSink"/> class.
        /// </summary>
        /// <param name="logger">The Microsoft test logger to sink the log messages to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public MicrosoftILoggerLogSink(ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a Microsoft test logger to sink the log messages to");
            _logger = logger;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            string message = logEvent.RenderMessage();
            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose: 
                    _logger.LogTrace(message);
                    break;
                case LogEventLevel.Debug:
                    _logger.LogDebug(message);
                    break;
                case LogEventLevel.Information:
                    _logger.LogInformation(message);
                    break;
                case LogEventLevel.Warning:
                    _logger.LogWarning(message);
                    break;
                case LogEventLevel.Error:
                    _logger.LogError(message);
                    break;
                case LogEventLevel.Fatal:
                    _logger.LogCritical(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logEvent.Level), logEvent.Level, "Unknown Serilog log level");
            }
        }
    }
}
