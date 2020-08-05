﻿using System;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Logging.Correlation 
{
    /// <summary>
    /// Correlate the incoming request with the outgoing response by using previously configured <see cref="CorrelationInfoOptions"/>.
    /// </summary>
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CorrelationService _service;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next functionality in the request pipeline to be executed.</param>
        /// <param name="service">The service to run the correlation functionality.</param>
        /// <param name="logger">The instance to log diagnostic messages during correlation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="next"/> or <paramref name="service"/> is <c>null</c>.</exception>
        public CorrelationMiddleware(
            RequestDelegate next,
            CorrelationService service,
            ILogger<CorrelationMiddleware> logger)
        {
            Guard.NotNull(next, nameof(next), "Requires a continuation delegate");
            Guard.NotNull(service, nameof(service), "Requires the HTTP correlation service");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance");

            _next = next;
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext" /> for the current request.</param>
        /// <returns>
        ///     A <see cref="Task" /> that represents the execution of this middleware.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="httpContext"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="httpContext"/> response headers are <c>null</c>.</exception>
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
                _logger.LogError("Unable to correlate the incoming request, returning 400 BadRequest (reason: {ErrorMessage})", errorMessage);

                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync(errorMessage);
            }
        }
    }
}