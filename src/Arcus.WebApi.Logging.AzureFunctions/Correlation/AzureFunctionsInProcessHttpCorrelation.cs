using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// Represents an <see cref="HttpCorrelationTemplate{THttpRequest,THttpResponse}"/> implementation
    /// that extracts and sets HTTP correlation throughout Azure Functions (in-process) HTTP trigger applications.
    /// </summary>
    public class AzureFunctionsInProcessHttpCorrelation
    {
        private readonly HttpCorrelationInfoOptions _options;
        private readonly IHttpCorrelationInfoAccessor _correlationInfoAccessor;
        private readonly ILogger<AzureFunctionsInProcessHttpCorrelation> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsInProcessHttpCorrelation" /> class.
        /// </summary>
        public AzureFunctionsInProcessHttpCorrelation(
            HttpCorrelationInfoOptions options,
            IHttpCorrelationInfoAccessor correlationInfoAccessor,
            ILogger<AzureFunctionsInProcessHttpCorrelation> logger)
        {
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
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        public void AddCorrelationResponseHeaders(HttpContext httpContext)
        {
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
                    StringValues traceParnet = httpContext.Request.Headers.GetTraceParent();
                    if (string.IsNullOrWhiteSpace(traceParnet))
                    {
                        _logger.LogTrace("No response header was added given no operation parent ID was found");
                    }
                    else
                    {
                        AddResponseHeader(httpContext, "traceparent", traceParnet);
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
