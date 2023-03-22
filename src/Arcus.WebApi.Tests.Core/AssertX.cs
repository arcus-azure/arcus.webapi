using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;

namespace Arcus.WebApi.Tests.Core
{
    public static class AssertX
    {
        public static RequestTelemetry GetRequestFrom(
            IEnumerable<ITelemetry> telemetries,
            Predicate<RequestTelemetry> filter)
        {
            return (RequestTelemetry) Assert.Single(telemetries, t => t is RequestTelemetry r && filter(r));
        }

        public static DependencyTelemetry GetDependencyFrom(
            IEnumerable<ITelemetry> telemetries,
            Predicate<DependencyTelemetry> filter)
        {
            return (DependencyTelemetry) Assert.Single(telemetries, t => t is DependencyTelemetry r && filter(r));
        }
    }
}
