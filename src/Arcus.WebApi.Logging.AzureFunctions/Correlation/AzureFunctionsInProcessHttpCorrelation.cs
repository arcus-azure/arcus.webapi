using System;
using System.Diagnostics;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// Represents an <see cref="HttpCorrelationTemplate{THttpRequest,THttpResponse}"/> implementation
    /// that extracts and sets HTTP correlation throughout Azure Functions (in-process) HTTP trigger applications.
    /// </summary>
    public class AzureFunctionsInProcessHttpCorrelation : HttpCorrelation
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IHttpCorrelationInfoAccessor _correlationInfoAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsInProcessHttpCorrelation"/> class.
        /// </summary>
        /// <param name="telemetryClient">The client instance to send out automatic dependency telemetry for built-in Microsoft dependencies.</param>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="httpContextAccessor">The instance to have access to the current HTTP context.</param>
        /// <param name="correlationInfoAccessor">The instance to set and retrieve the <see cref="CorrelationInfo"/> instance.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <exception cref="ArgumentNullException">When any of the parameters are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="options"/> doesn't contain a non-<c>null</c> <see cref="IOptions{TOptions}.Value"/></exception>
        public AzureFunctionsInProcessHttpCorrelation(
            TelemetryClient telemetryClient,
            HttpCorrelationInfoOptions options,
            IHttpContextAccessor httpContextAccessor,
            IHttpCorrelationInfoAccessor correlationInfoAccessor,
            ILogger<HttpCorrelation> logger)
            : base(Options.Create(options), httpContextAccessor, correlationInfoAccessor, logger)
        {
            Guard.NotNull(telemetryClient, nameof(telemetryClient), "Requires a telemetry client to automatically track built-in Microsoft dependencies");
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor), "Requires a HTTP correlation accessor to get/set the determined HTTP correlation from incoming HTTP requests");
            
            _telemetryClient = telemetryClient;
            _correlationInfoAccessor = correlationInfoAccessor;
        }

        /// <summary>
        /// Correlate the incoming HTTP request for the W3C standard for non-existing upstream service parent.
        /// </summary>
        /// <param name="requestHeaders">The HTTP request headers of the incoming HTTP request.</param>
        /// <returns>
        ///     An <see cref="HttpCorrelationResult"/> that reflects whether or not the incoming HTTP request was successfully correlated into a <see cref="CorrelationInfo"/> model
        ///     that is set into the application's <see cref="IHttpCorrelationInfoAccessor"/>.
        /// </returns>
        protected override HttpCorrelationResult CorrelateW3CForNewParent(IHeaderDictionary requestHeaders)
        {
            string transactionId = ActivityTraceId.CreateRandom().ToHexString();
            Logger.LogTrace("Correlation transaction ID '{TransactionId}' found in 'traceparent' HTTP request header", transactionId);

            var result = HttpCorrelationResult.Success(_telemetryClient, transactionId, operationParentId: null);
            _correlationInfoAccessor.SetCorrelationInfo(result.CorrelationInfo);

            return result;
        }

        /// <summary>
        /// Correlate the incoming HTTP request for the W3C standard for existing upstream service parent.
        /// </summary>
        /// <param name="requestHeaders">The HTTP request headers of the incoming HTTP request.</param>
        /// <returns>
        ///     An <see cref="HttpCorrelationResult"/> that reflects whether or not the incoming HTTP request was successfully correlated into a <see cref="CorrelationInfo"/> model
        ///     that is set into the application's <see cref="IHttpCorrelationInfoAccessor"/>.
        /// </returns>
        protected override HttpCorrelationResult CorrelateW3CForExistingParent(IHeaderDictionary requestHeaders)
        {
            // Format example:   00-4b1c0c8d608f57db7bd0b13c88ef865e-4c6893cc6c6cad10-00
            // Format structure: 00-<-----trace/transaction-id----->-<span/parent-id>-00 
            string traceParent = requestHeaders.GetTraceParent();
            string transactionId = ActivityTraceId.CreateFromString(traceParent.AsSpan(3, 32)).ToHexString();
            Logger.LogTrace("Correlation transaction ID '{TransactionId}' found in 'traceparent' HTTP request header", transactionId);

            var parentSpanId = ActivitySpanId.CreateFromString(traceParent.AsSpan(36, 16));
            string operationParentId = parentSpanId.ToHexString();
            Logger.LogTrace("Correlation operation parent ID '{OperationParentId}' found in 'traceparent' HTTP request header", operationParentId);

            var result = HttpCorrelationResult.Success(_telemetryClient, transactionId, operationParentId);
            _correlationInfoAccessor.SetCorrelationInfo(result.CorrelationInfo);

            return result;
        }
    }
}
