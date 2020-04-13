using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    /// <summary>
    /// Test middleware <see cref="RequestTrackingMiddleware"/> implementation that doesn't extract request body.
    /// </summary>
    public class NoBodyRequestTrackingMiddleware : RequestTrackingMiddleware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next pipeline function to process the HTTP context.</param>
        /// <param name="logger">The logger to write diagnostic messages during the request tracking.</param>
        public NoBodyRequestTrackingMiddleware(RequestDelegate next, ILogger<RequestTrackingMiddleware> logger)
            : base(next, logger)
        {
        }

        /// <summary>
        /// Extracts information from the given HTTP request body <paramref name="requestStream"/> to include in the request tracking context.
        /// </summary>
        /// <param name="requestStream">The body of the current HTTP request.</param>
        protected override Task<IDictionary<string, object>> ExtractRequestBodyAsync(Stream requestStream)
        {
            return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>());
        }
    }
}
