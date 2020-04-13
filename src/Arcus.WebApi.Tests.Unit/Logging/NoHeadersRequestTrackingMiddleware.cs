using System.Collections.Generic;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    /// <summary>
    /// Test middleware <see cref="RequestTrackingMiddleware"/> implementation that doesn't extract request headers.
    /// </summary>
    public class NoHeadersRequestTrackingMiddleware : RequestTrackingMiddleware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next pipeline function to process the HTTP context.</param>
        /// <param name="logger">The logger to write diagnostic messages during the request tracking.</param>
        public NoHeadersRequestTrackingMiddleware(
            RequestDelegate next,
            ILogger<RequestTrackingMiddleware> logger) 
            : base(next, logger)
        {
        }

        /// <summary>
        /// Extracts information from the given HTTP <paramref name="requestHeaders"/> to include in the request tracking context.
        /// </summary>
        /// <param name="requestHeaders">The headers of the current HTTP request.</param>
        protected override IDictionary<string, object> ExtractRequestHeaders(IHeaderDictionary requestHeaders)
        {
            return new Dictionary<string, object>();
        }
    }
}
