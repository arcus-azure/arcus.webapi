using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Converters;
using Microsoft.ApplicationInsights.Channel;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    public class InMemoryApplicationInsightsTelemetryConverter : TelemetryConverterBase
    {
        private readonly ApplicationInsightsTelemetryConverter _telemetryConverter;
        private readonly ICollection<ITelemetry> _telemetries = new Collection<ITelemetry>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryApplicationInsightsTelemetryConverter" /> class.
        /// </summary>
        public InMemoryApplicationInsightsTelemetryConverter()
        {
            _telemetryConverter = ApplicationInsightsTelemetryConverter.Create();
        }

        public ITelemetry[] Telemetries => _telemetries.ToArray();

        public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
        {
            IEnumerable<ITelemetry> telemetries = _telemetryConverter.Convert(logEvent, formatProvider);
            foreach (ITelemetry telemetry in telemetries)
            {
                _telemetries.Add(telemetry);
            }

            return Enumerable.Empty<ITelemetry>();
        }
    }
}
