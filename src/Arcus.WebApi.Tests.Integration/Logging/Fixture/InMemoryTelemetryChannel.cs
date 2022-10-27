using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.ApplicationInsights.Channel;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    public class InMemoryTelemetryChannel : ITelemetryChannel
    {
        private readonly ICollection<ITelemetry> _telemetries = new Collection<ITelemetry>();

        public ITelemetry[] Telemetries => _telemetries.ToArray();
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }

        public void Send(ITelemetry item)
        {
            _telemetries.Add(item);
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}