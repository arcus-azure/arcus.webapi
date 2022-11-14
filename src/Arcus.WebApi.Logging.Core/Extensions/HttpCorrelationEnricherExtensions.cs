using System;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Serilog.Enrichers;
using Arcus.WebApi.Logging.Core.Correlation;
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
            Guard.NotNull(enrichmentConfiguration, nameof(enrichmentConfiguration), "Requires a Serilog logger enrichment configuration to register the HTTP correlation as enrichment");
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider to retrieve the HTTP correlation from the registered services when enriching the Serilog with the HTTP correlation");

            var correlationInfoAccessor = serviceProvider.GetService<IHttpCorrelationInfoAccessor>();
            if (correlationInfoAccessor is null)
            {
                throw new InvalidOperationException(
                    $"Cannot register the HTTP correlation as a Serilog enrichment because no {nameof(IHttpCorrelationInfoAccessor)} was available in the registered services," 
                    + "please make sure to call 'services.AddHttpCorrelation()' when configuring the services. " 
                    + "For more information on HTTP correlation, see the official documentation: https://webapi.arcus-azure.net/features/correlation");
            }

            return enrichmentConfiguration.WithCorrelationInfo(correlationInfoAccessor);
        }
    }
}
