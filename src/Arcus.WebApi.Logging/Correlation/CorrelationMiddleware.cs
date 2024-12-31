using System;
using System.Threading.Tasks;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Logging.Correlation 
{
    /// <summary>
    /// Correlate the incoming request with the outgoing response by using previously configured <see cref="HttpCorrelationInfoOptions"/>.
    /// </summary>
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next functionality in the request pipeline to be executed.</param>
        /// <param name="logger">The instance to log diagnostic messages during correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="next"/> is <c>null</c>.</exception>
        public CorrelationMiddleware(
            RequestDelegate next,
            ILogger<CorrelationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next), "Requires a continuation delegate");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Requires a logger instance");
        }

        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext" /> for the current request.</param>
        /// <param name="service">The service to run the correlation functionality.</param>
        /// <returns>
        ///     A <see cref="Task" /> that represents the execution of this middleware.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="httpContext"/> or <paramref name="service"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="httpContext"/> response headers are <c>null</c>.</exception>
        public async Task Invoke(HttpContext httpContext, HttpCorrelation service)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext), "Requires a HTTP context");
            }
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service), "Requires the HTTP correlation service");
            }
            if (httpContext.Response is null)
            {
                throw new ArgumentException("Requires a 'Response'", nameof(httpContext));
            }
            if (httpContext.Response.Headers is null)
            {
                throw new ArgumentException("Requires a 'Response' object with headers", nameof(httpContext));
            }

            using (HttpCorrelationResult result = service.CorrelateHttpRequest())
            {
                if (result.IsSuccess)
                {
                    await _next(httpContext);
                }
                else
                {
                    _logger.LogError("Unable to correlate the incoming request, returning 400 BadRequest (reason: {ErrorMessage})", result.ErrorMessage);

                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync(result.ErrorMessage);
                }
            }
        }
    }
}
