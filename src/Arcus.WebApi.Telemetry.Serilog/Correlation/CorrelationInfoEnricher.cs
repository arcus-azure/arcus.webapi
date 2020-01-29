using System;
using Arcus.WebApi.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Arcus.WebApi.Telemetry.Serilog.Correlation
{
    /// <summary>
    /// Enriches the log events with the correlation information.
    /// </summary>
    public class CorrelationInfoEnricher : IDiagnosticEnricher
    {
        private const string TransactionIdProperty = "TransactionId",
                             OperationIdProperty = "OperationId";

        /// <summary>
        /// Enrich the Serilog request logging <paramref name="diagnosticContext"/> with the <paramref name="httpContext"/>.
        /// </summary>
        /// <param name="diagnosticContext">The context to enrich.</param>
        /// <param name="httpContext">The current context in the request pipeline.</param>
        public void Enrich(IDiagnosticContext diagnosticContext, HttpContext httpContext)
        {
            Guard.NotNull(diagnosticContext, nameof(diagnosticContext));
            Guard.NotNull(httpContext, nameof(httpContext));

            var correlationInfo = httpContext.Features.Get<CorrelationInfo>();

            if (!String.IsNullOrEmpty(correlationInfo.TransactionId))
            {
                diagnosticContext.Set(TransactionIdProperty, correlationInfo.TransactionId);
            }

            if (!String.IsNullOrEmpty(correlationInfo.OperationId))
            {
                diagnosticContext.Set(OperationIdProperty, correlationInfo.OperationId);
            }
        }
    }
}
