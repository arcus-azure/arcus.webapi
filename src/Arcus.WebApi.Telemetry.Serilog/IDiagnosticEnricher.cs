using Microsoft.AspNetCore.Http;
using Serilog;

namespace Arcus.WebApi.Telemetry.Serilog
{
    /// <summary>
    /// Represents a custom enrichment of the Serilog request logging.
    /// </summary>
    public interface IDiagnosticEnricher
    {
        /// <summary>
        /// Enrich the Serilog request logging <paramref name="diagnosticContext"/> with the <paramref name="httpContext"/>.
        /// </summary>
        /// <param name="diagnosticContext">The context to enrich.</param>
        /// <param name="httpContext">The current context in the request pipeline.</param>
        void Enrich(IDiagnosticContext diagnosticContext, HttpContext httpContext);
    }
}
