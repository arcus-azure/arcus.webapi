using Arcus.WebApi.Correlation;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;

namespace Arcus.WebApi.Telemetry.Serilog.Correlation
{
    /// <summary>
    /// Extensions on the <see cref="LoggerEnrichmentConfiguration "/> to add the correlation information enricher.
    /// </summary>
    public static class LoggerEnrichmentConfigurationExtensions
    {
        /// <summary>
        /// Adds the <see cref="CorrelationInfoEnricher"/> to the <paramref name="enrichmentConfiguration"/> so the log entries will contain the correlation information in log properties.
        /// </summary>
        /// <param name="enrichmentConfiguration">The configuration containing all the additional enrichers for the log entries.</param>
        /// <param name="correlationInfo">The correlation information registered in the <see cref="IServiceCollection"/>.</param>
        public static LoggerConfiguration WithCorrelation(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            HttpCorrelationInfo correlationInfo)
        {
            Guard.NotNull(enrichmentConfiguration, nameof(enrichmentConfiguration));
            Guard.NotNull(correlationInfo, nameof(correlationInfo));

            return enrichmentConfiguration.With(new CorrelationInfoEnricher(correlationInfo));
        }
    }
}
