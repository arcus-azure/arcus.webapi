using System.Collections.Generic;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// Test middleware <see cref="RequestTrackingMiddleware"/> implementation that doesn't extract request headers.
    /// </summary>
    public class NoHeadersRequestTrackingMiddleware : RequestTrackingMiddleware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingMiddleware"/> class.
        /// </summary>
        /// <param name="options">The options to control the behavior of the request tracking.</param>
        /// <param name="next">The next pipeline function to process the HTTP context.</param>
        /// <param name="logger">The logger to write diagnostic messages during the request tracking.</param>
        public NoHeadersRequestTrackingMiddleware(RequestTrackingOptions options, RequestDelegate next, ILogger<RequestTrackingMiddleware> logger) 
            : base(options, next, logger) { }

        /// <summary>
        /// Extracts information from the given HTTP <paramref name="requestHeaders"/> to include in the request tracking context.
        /// </summary>
        /// <param name="requestHeaders">The headers of the current HTTP request.</param>
        protected override IDictionary<string, StringValues> SanitizeRequestHeaders(IDictionary<string, StringValues> requestHeaders)
        {
            return new Dictionary<string, StringValues>();
        }
    }
}
