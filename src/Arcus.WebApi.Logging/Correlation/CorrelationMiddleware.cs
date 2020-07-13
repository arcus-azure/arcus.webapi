using System;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;

namespace Arcus.WebApi.Logging.Correlation 
{
    /// <summary>
    /// Correlate the incoming request with the outgoing response by using previously configured options.
    /// </summary>
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HttpCorrelationService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next functionality in the request pipeline to be executed.</param>
        /// <param name="service">The service to run the correlation functionality.</param>
        public CorrelationMiddleware(
            RequestDelegate next,
            HttpCorrelationService service)
        {
            Guard.NotNull(next, nameof(next), "Requires a continuation delegate");
            Guard.NotNull(service, nameof(service), "Requires the HTTP correlation service");

            _next = next;
            _service = service;
        }

        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="httpContext">The <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> for the current request.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the execution of this middleware.</returns>
        public async Task Invoke(HttpContext httpContext)
        {
            Guard.NotNull(httpContext, nameof(httpContext));
            Guard.For<ArgumentException>(() => httpContext.Response is null, "Requires a 'Response'");
            Guard.For<ArgumentException>(() => httpContext.Response.Headers is null, "Requires a 'Response' object with headers");

            if (_service.TryHttpCorrelate(out string errorMessage))
            {
                await _next(httpContext);
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync(errorMessage);
            }
        }
    }
}