using System;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next functionality in the request pipeline to be executed.</param>
        /// <param name="options">The options controlling how the correlation should happen.</param>
        /// <param name="logger">The logger to trace diagnostic messages during the correlation.</param>
        /// <exception cref="ArgumentNullException">When any of the parameters are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="options"/> doesn't contain a non-<c>null</c> <see cref="IOptions{TOptions}.Value"/></exception>
        public CorrelationMiddleware(
            RequestDelegate next,
            IOptions<CorrelationOptions> options,
            ILogger<CorrelationMiddleware> logger)
        {
            Guard.NotNull(next, nameof(next), "Requires a continuation delegate");
            Guard.NotNull(options, nameof(options), "Requires a set of correlation options to manipulate how the correlation should happen");
            Guard.NotNull(logger, nameof(logger), "Requires a logging implementation to trace diagnostic messages during the correlation");
            Guard.For<ArgumentException>(() => options.Value is null, "Requires a set of correlation options to manipulate how the correlation should happen");

            _next = next;
            _options = options.Value;
            _logger = logger;
        }
        
        /// <summary>Request handling method.</summary>
        /// <param name="httpContext">The <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> for the current request.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the execution of this middleware.</returns>
        public async Task Invoke(HttpContext httpContext)
        {
            Guard.NotNull(httpContext, nameof(httpContext));
            Guard.For<ArgumentException>(() => httpContext.Response is null, "Requires a 'Response'");
            Guard.For<ArgumentException>(() => httpContext.Response.Headers is null, "Requires a 'Response' object with headers");

            if (httpContext.Request.Headers.TryGetValue(_options.Transaction.HeaderName, out StringValues transactionIds))
            {
                if (!_options.Transaction.AllowInRequest)
                {
                    _logger.LogError("No correlation request header '{HeaderName}' for transaction ID was allowed in request", _options.Transaction.HeaderName);
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync($"No correlation transaction ID request header '{_options.Transaction.HeaderName}' was allowed in the request");

                    return;
                }

                _logger.LogTrace("Correlation request header '{HeaderName}' found with transaction ID '{TransactionId}'", _options.Transaction.HeaderName, transactionIds);
            }

            string operationId = DetermineOperationId(httpContext);
            string transactionId = DetermineTransactionId(httpContext, transactionIds);
            var correlation = new CorrelationInfo(operationId, transactionId);
            httpContext.Features.Set(correlation);

            AddCorrelationResponseHeaders(httpContext, operationId, transactionId);

            // TODO: on exception, should we enrich the exception.Data with the correlation info?
            await _next(httpContext);
        }

        private static string DetermineOperationId(HttpContext httpContext)
        {
            // TODO: make ID generation configurable for the consumer.
            return httpContext.TraceIdentifier ?? Guid.NewGuid().ToString();
        }

        private string DetermineTransactionId(HttpContext httpContext, StringValues transactionIds)
        {
            // TODO: make ID generation configurable for the consumer.
            if (String.IsNullOrWhiteSpace(transactionIds.ToString()))
            {
                if (_options.Transaction.GenerateWhenNotSpecified)
                {
                    return Guid.NewGuid().ToString();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return transactionIds.ToString();
            }
        }

        private void AddCorrelationResponseHeaders(HttpContext httpContext, string operationId, string transactionId)
        {
            if (_options.Operation.IncludeInResponse)
            {
                httpContext.Response.OnStarting(() =>
                {
                    AddResponseHeader(httpContext, _options.Operation.HeaderName, operationId);
                    return Task.CompletedTask;
                });
            }

            if (_options.Transaction.IncludeInResponse)
            {
                httpContext.Response.OnStarting(() =>
                {
                    AddResponseHeader(httpContext, _options.Transaction.HeaderName, transactionId);
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