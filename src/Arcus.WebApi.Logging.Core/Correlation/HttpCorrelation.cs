using System;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Arcus.WebApi.Logging.Correlation
{
    /// <summary>
    /// Provides the functionality to correlate HTTP requests and responses according to configured <see cref="HttpCorrelationInfoOptions"/>,
    /// using the <see cref="ICorrelationInfoAccessor"/> to expose the result.
    /// </summary>
    /// <seealso cref="HttpCorrelationInfoAccessor"/>
    public class HttpCorrelation : HttpCorrelationTemplate<HttpRequest, HttpResponse>, ICorrelationInfoAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpCorrelationInfoOptions _options;
        private readonly ICorrelationInfoAccessor<CorrelationInfo> _correlationInfoAccessor;
        private readonly ILogger<HttpCorrelation> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelation"/> class.
        /// </summary>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="correlationInfoAccessor">The instance to set and retrieve the <see cref="CorrelationInfo"/> instance.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <param name="httpContextAccessor">The instance to have access to the current HTTP context.</param>
        /// <exception cref="ArgumentNullException">When any of the parameters are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="options"/> doesn't contain a non-<c>null</c> <see cref="IOptions{TOptions}.Value"/></exception>
        public HttpCorrelation(
            IOptions<HttpCorrelationInfoOptions> options,
            IHttpContextAccessor httpContextAccessor,
            IHttpCorrelationInfoAccessor correlationInfoAccessor,
            ILogger<HttpCorrelation> logger)
            : base(options?.Value, correlationInfoAccessor, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a value in the set of options to configure the correlation process");
            }

            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(paramName: nameof(httpContextAccessor), message: "Requires a HTTP context accessor to get the current HTTP context");
            _correlationInfoAccessor = correlationInfoAccessor ?? throw new ArgumentNullException(paramName: nameof(correlationInfoAccessor), message: "Requires a correlation info instance to set and retrieve the correlation information");
            _options = options.Value ?? throw new ArgumentNullException(paramName: nameof(options.Value), message: "Requires a value in the set of options to configure the correlation process");


            _logger = logger ?? NullLogger<HttpCorrelation>.Instance;
        }

        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            return _correlationInfoAccessor.GetCorrelationInfo();
        }

        /// <summary>
        /// Sets the current correlation information for this context.
        /// </summary>
        /// <param name="correlationInfo">The correlation model to set.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="correlationInfo"/> is <c>null</c>.</exception>
        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
            if (_correlationInfoAccessor is null)
            {
                throw new ArgumentNullException(nameof(correlationInfo), "Requires a correlation info instance to set and retrieve the correlation information");
            }

            _correlationInfoAccessor.SetCorrelationInfo(correlationInfo);
        }

        /// <summary>
        /// Correlate the current HTTP request according to the previously configured <see cref="HttpCorrelationInfoOptions"/>;
        /// returning an <see cref="HttpCorrelationResult"/> which acts as the current scope in which additional dependencies should be tracked.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the given <see cref="HttpContext"/> is not available to correlate the request with the response.</exception>
        /// <exception cref="ArgumentException">Thrown when the given <see cref="HttpContext"/> doesn't have any response headers to set the correlation headers.</exception>
        public HttpCorrelationResult CorrelateHttpRequest()
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext ?? throw new ArgumentNullException(paramName: nameof(HttpContext), "Requires a HTTP context from the HTTP context accessor to start correlating the HTTP request");

            if (httpContext.Response is null)
            {
                throw new ArgumentException("Requires a 'Response'", nameof(httpContext.Response));
            }

            if (httpContext.Response.Headers is null)
            {
                throw new ArgumentException("Requires a 'Response' object with headers", nameof(httpContext.Response.Headers));
            }

            HttpCorrelationResult result = TrySettingCorrelationFromRequest(httpContext.Request, httpContext.TraceIdentifier);
            if (result.IsSuccess)
            {
                AddCorrelationResponseHeaders(httpContext, result.RequestId);
            }

            return result;
        }

        /// <summary>
        /// Gets the HTTP request headers from the incoming <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The incoming HTTP request.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        protected override IHeaderDictionary GetRequestHeaders(HttpRequest request)
        {
            return request.Headers;
        }

        /// <summary>
        /// Set the <paramref name="headerName"/>, <paramref name="headerValue"/> combination in the outgoing <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The outgoing HTTP response that gets a HTTP correlation header.</param>
        /// <param name="headerName">The HTTP correlation response header name.</param>
        /// <param name="headerValue">The HTTP correlation response header value.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="response"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> or <paramref name="headerValue"/> is blank.</exception>
        protected override void SetHttpResponseHeader(HttpResponse response, string headerName, string headerValue)
        {
            throw new NotImplementedException($"The {nameof(HttpCorrelation)} does the HTTP correlation setting internally");
        }

        private void AddCorrelationResponseHeaders(HttpContext httpContext, string requestId)
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
                    if (string.IsNullOrWhiteSpace(requestId))
                    {
                        _logger.LogTrace("No response header was added given no operation parent ID was found");
                    }
                    else
                    {
                        AddResponseHeader(httpContext, _options.UpstreamService.HeaderName, requestId);
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
