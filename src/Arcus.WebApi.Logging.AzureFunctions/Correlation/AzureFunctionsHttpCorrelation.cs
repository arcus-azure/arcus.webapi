using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// Represents an <see cref="HttpCorrelationTemplate{THttpRequest,THttpResponse}"/> implementation
    /// that extracts and sets HTTP correlation throughout Azure Functions (isolated) HTTP trigger applications.
    /// </summary>
    public class AzureFunctionsHttpCorrelation : HttpCorrelationTemplate<HttpRequestData, HttpResponseData>
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IHttpCorrelationInfoAccessor _correlationInfoAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsHttpCorrelation" /> class that uses W3C HTTP correlation.
        /// </summary>
        /// <param name="telemetryClient">The client instance to send out automatic dependency telemetry for built-in Microsoft dependencies.</param>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="correlationInfoAccessor">The instance to set and retrieve the <see cref="CorrelationInfo"/> instance.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="telemetryClient"/>, <paramref name="options"/> or <paramref name="correlationInfoAccessor"/> is <c>null</c>.</exception>
        public AzureFunctionsHttpCorrelation(
            TelemetryClient telemetryClient,
            HttpCorrelationInfoOptions options, 
            IHttpCorrelationInfoAccessor correlationInfoAccessor, 
            ILogger<AzureFunctionsHttpCorrelation> logger) 
            : base(options, correlationInfoAccessor, logger)
        {
            _telemetryClient = telemetryClient;
            _correlationInfoAccessor = correlationInfoAccessor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsHttpCorrelation" /> class that uses Hierarchical HTTP correlation.
        /// </summary>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="correlationInfoAccessor">The instance to set and retrieve the <see cref="CorrelationInfo"/> instance.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="correlationInfoAccessor"/> is <c>null</c>.</exception>
        public AzureFunctionsHttpCorrelation(
            HttpCorrelationInfoOptions options, 
            IHttpCorrelationInfoAccessor correlationInfoAccessor, 
            ILogger<AzureFunctionsHttpCorrelation> logger) 
            : base(options, correlationInfoAccessor, logger)
        {
        }

        /// <summary>
        /// Gets the HTTP request headers from the incoming <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The incoming HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        protected override IHeaderDictionary GetRequestHeaders(HttpRequestData request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request), "Requires a HTTP request instance to retrieve the HTTP request headers");
            }

            Dictionary<string, StringValues> dictionary = 
                request.Headers.ToDictionary(
                p => p.Key,
                p => new StringValues(p.Value.ToArray()));
            
            return new HeaderDictionary(dictionary);
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

            var result = HttpCorrelationResult.Success(_telemetryClient, transactionId);
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

            var result = HttpCorrelationResult.Success(_telemetryClient, transactionId, operationParentId, traceParent);
            _correlationInfoAccessor.SetCorrelationInfo(result.CorrelationInfo);

            return result;
        }

        /// <summary>
        /// Set the <paramref name="headerName"/>, <paramref name="headerValue"/> combination in the outgoing <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The outgoing HTTP response that gets a HTTP correlation header.</param>
        /// <param name="headerName">The HTTP correlation response header name.</param>
        /// <param name="headerValue">The HTTP correlation response header value.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="response"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> or <paramref name="headerValue"/> is blank.</exception>
        protected override void SetHttpResponseHeader(HttpResponseData response, string headerName, string headerValue)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response), "Requires a HTTP response to set the HTTP correlation headers");
            }
            if (string.IsNullOrWhiteSpace(headerName))
            {
                throw new ArgumentException("Requires a non-blank HTTP correlation header name to set the HTTP correlation header in the HTTP request", nameof(headerName));
            }
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                throw new ArgumentException("Requires a non-blank HTTP correlation header value to set the HTTP correlation header in the HTTP request", nameof(headerValue));
            }

            response.Headers.Add(headerName, headerValue);
        }
    }
}
