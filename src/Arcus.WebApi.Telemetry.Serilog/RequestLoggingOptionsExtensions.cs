using System;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.AspNetCore;

namespace Arcus.WebApi.Telemetry.Serilog
{
    /// <summary>
    /// Adds telemetry extensions for the custom <see cref="IDiagnosticEnricher"/> Serilog request logging.
    /// </summary>
    public static class RequestLoggingOptionsExtensions
    {
        /// <summary>
        /// Adds a custom <see cref="IDiagnosticEnricher"/> implementation to the Serilog request logging.
        /// </summary>
        /// <param name="options">The options to add the enrichment to.</param>
        /// <param name="enricher">The custom implementation that enriches the request logging.</param>
        public static RequestLoggingOptions WithDiagnosticEnricher(this RequestLoggingOptions options, IDiagnosticEnricher enricher)
        {
            Guard.NotNull(enricher, nameof(enricher));

            Action<IDiagnosticContext, HttpContext> currentEnrichment = options.EnrichDiagnosticContext;
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                currentEnrichment?.Invoke(diagnosticContext, httpContext);
                enricher.Enrich(diagnosticContext, httpContext);
            };

            return options;
        }
    }
}
