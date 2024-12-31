using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// Represents a component to disable the <see cref="HttpContext.TraceIdentifier"/> from the request information.
    /// </summary>
    public class TraceIdentifierMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TraceIdentifierOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceIdentifierMiddleware"/> class.
        /// </summary>
        public TraceIdentifierMiddleware(
            RequestDelegate next,
            TraceIdentifierOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next), "Requires a continuation delegate");
            _options = options ?? throw new ArgumentNullException(nameof(options), $"Requires a non-null '{nameof(TraceIdentifierOptions)}' options instance");
        }

        /// <summary>Request handling method.</summary>
        /// <param name="httpContext">The <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> for the current request.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the execution of this middleware.</returns>
        public async Task Invoke(HttpContext httpContext)
        {
            if (!_options.EnableTraceIdentifier)
            {
                httpContext.TraceIdentifier = String.Empty;
            }

            await _next(httpContext);
        }
    }
}
