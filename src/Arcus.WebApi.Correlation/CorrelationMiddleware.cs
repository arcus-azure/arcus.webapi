using System;
using System.Threading.Tasks;
using Correlate;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using static Correlate.Http.CorrelationHttpHeaders;

namespace Arcus.WebApi.Correlation 
{
    /// <summary>
    /// Correlate the incoming request with the outgoing response by using previously configured options.
    /// </summary>
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CorrelationOptions _options;
        private readonly ILogger<CorrelationMiddleware> _logger;
        private readonly ICorrelationAccessor _correlationContextAccessor;
        private readonly IAsyncCorrelationManager _asyncCorrelationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next request delegate to invoke in the request execution pipeline.</param>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <param name="correlationAccessor">The correlation accessor to store the correlation ID information.</param>
        /// <param name="asyncCorrelationManager">The correlation manager to continue the application pipeline in a scoped correlated environment.</param>
        /// <exception cref="ArgumentNullException">When any of the parameters are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="options"/> doesn't contain a non-<c>null</c> <see cref="IOptions{TOptions}.Value"/></exception>
        public CorrelationMiddleware(
            RequestDelegate next,
            IOptions<CorrelationOptions> options,
            ILogger<CorrelationMiddleware> logger,
            ICorrelationAccessor correlationAccessor,
            IAsyncCorrelationManager asyncCorrelationManager)
        {
            Guard.NotNull(next, nameof(next), "Requires a 'next' middleware delegate to continue the application pipeline");
            Guard.NotNull(options, nameof(options), "Requires a set of correlation options to manipulate how the correlation should happen");
            Guard.NotNull(logger, nameof(logger), "Requires a logging implementation to trace diagnostic messages during the correlation");
            Guard.NotNull(correlationAccessor, nameof(correlationAccessor), "Requires a correlation accessor implementation to store the correlation ID information");
            Guard.NotNull(asyncCorrelationManager, nameof(asyncCorrelationManager), "Requires a correlation manager to continue the application pipeline in a scoped correlated environment");
            Guard.For<ArgumentException>(() => options.Value is null, "Requires a set of correlation options to manipulate how the correlation should happen");

            _next = next;
            _options = options.Value;
            _logger = logger;
            _correlationContextAccessor = correlationAccessor;
            _asyncCorrelationManager = asyncCorrelationManager;
        }

        /// <summary>
        /// Invokes the middleware for the current <paramref name="httpContext"/>.
        /// </summary>
        /// <param name="httpContext">The current <see cref="HttpContext"/>.</param>
        /// <returns>An awaitable to wait for to complete the request.</returns>
        public Task Invoke(HttpContext httpContext)
        {
            Guard.NotNull(httpContext, nameof(httpContext));
            Guard.For<ArgumentException>(() => httpContext.Response is null, "Requires a 'Response'");
            Guard.For<ArgumentException>(() => httpContext.Response.Headers is null, "Requires a 'Response' object with headers");

            if (httpContext.Request.Headers.TryGetValue(CorrelationId, out StringValues headerValue))
            {
                _logger.LogTrace("Request header '{HeaderName}' found with correlation id '{CorrelationId}'.", CorrelationId, headerValue);
            }

            return _asyncCorrelationManager.CorrelateAsync(headerValue, () => CorrelateRequest(httpContext));
        }

        private Task CorrelateRequest(HttpContext httpContext)
        {
            if (_options.Transaction.IncludeInResponse)
            {
                string transactionId = _correlationContextAccessor.CorrelationId;
                httpContext.Response.OnStarting(() =>
                {
                    TryAddResponseHeader(httpContext, CorrelationId, transactionId);
                    return Task.CompletedTask;
                });
            }

            if (_options.Operation.IncludeInResponse)
            {
                string operationId = _correlationContextAccessor.RequestId;

                httpContext.Response.OnStarting(() =>
                {
                    TryAddResponseHeader(httpContext, RequestId, operationId);
                    return Task.CompletedTask;
                });
            }

            return _next(httpContext);
        }

        private void TryAddResponseHeader(HttpContext httpContext, string headerName, string headerValue)
        {
            if (!httpContext.Response.Headers.ContainsKey(headerName))
            {
                _logger.LogTrace("Setting response header '{HeaderName}' to '{CorrelationId}'.", headerName, headerValue);
                httpContext.Response.Headers.Add(headerName, headerValue);
            }
        }
    }
}