using System.Collections.Concurrent;
using Microsoft.ApplicationInsights.Channel;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    public class InMemoryTelemetryChannel : ITelemetryChannel
    {
        private readonly ConcurrentStack<ITelemetry> _telemetries = new ConcurrentStack<ITelemetry>();

        public ITelemetry[] Telemetries => _telemetries.ToArray();
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }

        public void Send(ITelemetry item)
        {
            _telemetries.Push(item);
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}