using System.Collections.Concurrent;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    /// <summary>
    /// An in-memory implementation for the Serilog logging.
    /// </summary>
    public class InMemorySink : ILogEventSink
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


        /// <summary>Emit the provided log event to the sink.</summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            _logEvents.Enqueue(logEvent);
        }
    }
}
