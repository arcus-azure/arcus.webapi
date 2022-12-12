using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arcus.Observability.Telemetry.Core;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// An in-memory implementation for the Serilog logging.
    /// </summary>
    public class InMemorySink : ILogEventSink, IDisposable
    {
        private readonly ConcurrentQueue<LogEvent> _logEvents = new ConcurrentQueue<LogEvent>();

        /// <summary>
        /// Gets the current emitted log events.
        /// </summary>
        public IEnumerable<LogEvent> DequeueLogEvents()
        {
            LogEvent[] logEvents = _logEvents.ToArray();
            _logEvents.Clear();

            return logEvents;
        }

        /// <summary>
        /// Gets a value that indicates whether or not Request log properties have been logged.
        /// </summary>
        /// <returns></returns>
        public bool HasRequestLogProperties()
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = DequeueLogEvents().SelectMany(ev => ev.Properties);

            var eventContexts = properties.Where(prop => prop.Key == ContextProperties.RequestTracking.RequestLogEntry);
            return eventContexts.Any();
        }

        /// <summary>
        /// Gets a dictionary that contains the logged Request log properties.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, LogEventPropertyValue> GetRequestLogProperties()
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties =
                DequeueLogEvents().SelectMany(ev => ev.Properties);

            var logEntries = properties.Where(prop => prop.Key == ContextProperties.RequestTracking.RequestLogEntry);
            (string key, LogEventPropertyValue logEntry) = logEntries.Single();
            var requestLogEntry = Assert.IsType<StructureValue>(logEntry);

            return requestLogEntry.Properties.ToDictionary(item => item.Name, item => item.Value);
        }

        /// <summary>
        /// Gets a dictionary that contains the custom logged context properties.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public IDictionary<string, string> GetLoggedEventContext()
        {
            var logProperties = GetRequestLogProperties();

            if (logProperties.ContainsKey(ContextProperties.TelemetryContext))
            {
                if (!(logProperties[ContextProperties.TelemetryContext] is DictionaryValue dictionaryValue))
                {
                    throw new InvalidDataException("TelemetryContext is not a DictionaryValue");
                }

                return dictionaryValue.Elements.ToDictionary(item => item.Key.ToStringValue(), item => item.Value.ToStringValue().Trim('\\', '\"', '[', ']'));
            }

            return new Dictionary<string, string>();
        }

        /// <summary>Emit the provided log event to the sink.</summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            _logEvents.Enqueue(logEvent);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _logEvents.Clear();
        }
    }
}
