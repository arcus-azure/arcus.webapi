using GuardNet;
using Serilog.AspNetCore;

namespace Arcus.WebApi.Telemetry.Serilog.Correlation
{
    /// <summary>
    /// Adds telemetry extensions for the correlation information.
    /// </summary>
    public static class RequestLoggingOptionsExtensions
    {
        /// <summary>
        /// Adds the correlation information to the Serilog request logging.
        /// </summary>
        /// <param name="options">The options to add the correlation to.</param>
        public static RequestLoggingOptions WithCorrelation(this RequestLoggingOptions options)
        {
            Guard.NotNull(options, nameof(options));

            return options.WithDiagnosticEnricher(new CorrelationInfoEnricher());
        }
    }
}
