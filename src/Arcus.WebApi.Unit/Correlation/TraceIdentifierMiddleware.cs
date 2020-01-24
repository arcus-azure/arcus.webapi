using System;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Arcus.WebApi.Unit.Correlation
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
            IOptions<TraceIdentifierOptions> options)
        {
            _next = next;
            Guard.NotNull(next, nameof(next), "Requires a continuation delegate");
            Guard.NotNull(options, nameof(options), $"Requires a non-null '{nameof(TraceIdentifierOptions)}' options");
            Guard.For<ArgumentException>(() => options.Value is null, $"Requires a value for the '{nameof(TraceIdentifierOptions)}' options");

            _options = options.Value;
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
