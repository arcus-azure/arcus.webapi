using System.Collections.Generic;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class CustomRequestTrackingMiddleware : AzureFunctionsRequestTrackingMiddleware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomRequestTrackingMiddleware" /> class.
        /// </summary>
        public CustomRequestTrackingMiddleware(RequestTrackingOptions options) : base(options)
        {
        }

        protected override IDictionary<string, StringValues> SanitizeRequestHeaders(IDictionary<string, StringValues> requestHeaders)
        {
            return new Dictionary<string, StringValues>();
        }

        protected override string SanitizeRequestBody(HttpRequestData request, string requestBody)
        {
            return $"custom[{requestBody}]";
        }

        protected override string SanitizeResponseBody(HttpResponseData response, string responseBody)
        {
            return $"custom[{responseBody}]";
        }
    }
}
