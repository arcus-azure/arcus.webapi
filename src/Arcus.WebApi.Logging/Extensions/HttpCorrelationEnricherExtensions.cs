using System;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Serilog.Enrichers;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Serilog.Configuration
{
    /// <summary>
    /// Adds additional enrichment extensions to the Serilog <see cref="LoggerEnrichmentConfiguration"/>.
    /// </summary>
    public static class HttpCorrelationEnricherExtensions
    {
        /// <summary>
        /// Adds the <see cref="CorrelationInfoEnricher{TCorrelationInfo}"/> to the logger enrichment configuration which adds the <see cref="CorrelationInfo"/> information
        /// from the current HTTP context, using the <see cref="HttpCorrelationInfoAccessor"/>.
        /// </summary>
        /// <param name="enrichmentConfiguration">The configuration to add the enricher.</param>
        /// <param name="serviceProvider">The provider to retrieve the <see cref="IHttpContextAccessor"/> service.</param>
        /// <remarks>
        ///     In order to use the <see cref="HttpCorrelationInfoAccessor"/>, it first has to be added to the <see cref="IServiceCollection"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="enrichmentConfiguration"/> or <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public static LoggerConfiguration WithHttpCorrelationInfo(this LoggerEnrichmentConfiguration enrichmentConfiguration, IServiceProvider serviceProvider)
        {
            Guard.NotNull(enrichmentConfiguration, nameof(enrichmentConfiguration));
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            return enrichmentConfiguration.WithCorrelationInfo(
                new HttpCorrelationInfoAccessor(
                    serviceProvider.GetRequiredService<IHttpContextAccessor>()));
        }
    }
}
