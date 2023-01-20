using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// Represents an <see cref="HttpCorrelationTemplate{THttpRequest,THttpResponse}"/> implementation
    /// that extracts and sets HTTP correlation throughout Azure Functions (in-process) HTTP trigger applications.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AzureFunctionsInProcessHttpCorrelation
    {
        private readonly HttpCorrelationInfoOptions _options;
        private readonly IHttpCorrelationInfoAccessor _correlationInfoAccessor;
        private readonly ILogger<AzureFunctionsInProcessHttpCorrelation> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsInProcessHttpCorrelation" /> class.
        /// </summary>
        /// <param name="options">The HTTP correlation options to determine where the correlation information should be added to the HTTP response headers.</param>
        /// <param name="correlationInfoAccessor">The HTTP correlation accessor instance to retrieve the current correlation information.</param>
        /// <param name="logger">The logging instance to write diagnostic trace messages while adding the correlation information to the HTTP response headers.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, <paramref name="correlationInfoAccessor"/>, or the <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public AzureFunctionsInProcessHttpCorrelation(
            HttpCorrelationInfoOptions options,
            IHttpCorrelationInfoAccessor correlationInfoAccessor,
            ILogger<AzureFunctionsInProcessHttpCorrelation> logger)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of HTTP correlation options to determine where the correlation information should be added to the HTTP response headers");
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor), "Requires a HTTP correlation accessor to retrieve the current correlation information");
            Guard.NotNull(logger, nameof(logger), "Requires a logging instance to write diagnostic trace messages while adding the correlation information to the HTTP response headers");

            _options = options;
            _correlationInfoAccessor = correlationInfoAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            return _correlationInfoAccessor.GetCorrelationInfo();
        }

        /// <summary>
        /// Adds the current correlation information to the HTTP response headers.
        /// </summary>
        /// <param name="httpContext">The current HTTP context to add the correlation response headers to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="httpContext"/> is <c>null</c> or does not have a response present.</exception>
        public void AddCorrelationResponseHeaders(HttpContext httpContext)
        {
            Guard.NotNull(httpContext, nameof(httpContext), "Requires a HTTP context to add the correlation information to the response headers");
            Guard.NotNull(httpContext.Response, nameof(httpContext), "Requires a HTTP response in the HTTP context to add the correlation information to the response headers");

            if (_options.Operation.IncludeInResponse)
            {
                _logger.LogTrace("Prepare for the operation ID to be included in the response...");
                httpContext.Response.OnStarting(() =>
                {
                    CorrelationInfo correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
                    if (string.IsNullOrWhiteSpace(correlationInfo?.OperationId))
                    {
                        _logger.LogWarning("No response header was added given no operation ID was found");
                    }
                    else
                    {
                        AddResponseHeader(httpContext, _options.Operation.HeaderName, correlationInfo.OperationId);
                    }

                    return Task.CompletedTask;
                });
            }

            if (_options.UpstreamService.IncludeInResponse)
            {
                _logger.LogTrace("Prepare for the operation parent ID to be included in the response...");
                httpContext.Response.OnStarting(() =>
                {
                    StringValues traceParent = httpContext.Request.Headers.GetTraceParent();
                    if (string.IsNullOrWhiteSpace(traceParent))
                    {
                        _logger.LogTrace("No response header was added given no operation parent ID was found");
                    }
                    else
                    {
                        AddResponseHeader(httpContext, "traceparent", traceParent);
                    }

                    return Task.CompletedTask;
                });
            }

            if (_options.Transaction.IncludeInResponse)
            {
                _logger.LogTrace("Prepare for the transactional correlation ID to be included in the response...");
                httpContext.Response.OnStarting(() =>
                {
                    CorrelationInfo correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
                    if (string.IsNullOrWhiteSpace(correlationInfo?.TransactionId))
                    {
                        _logger.LogWarning("No response header was added given no transactional correlation ID was found");
                    }
                    else
                    {
                        AddResponseHeader(httpContext, _options.Transaction.HeaderName, correlationInfo.TransactionId);
                    }

                    return Task.CompletedTask;
                });
            }
        }

        private void AddResponseHeader(HttpContext httpContext, string headerName, string headerValue)
        {
            _logger.LogTrace("Setting correlation response header '{HeaderName}' to '{CorrelationId}'", headerName, headerValue);
            httpContext.Response.Headers[headerName] = headerValue;
        }
    }
}
