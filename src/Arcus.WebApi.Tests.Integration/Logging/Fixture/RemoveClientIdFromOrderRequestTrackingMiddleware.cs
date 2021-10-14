using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    public class RemoveClientIdFromOrderRequestTrackingMiddleware : RequestTrackingMiddleware
    {
        public RemoveClientIdFromOrderRequestTrackingMiddleware(
            RequestTrackingOptions options, 
            RequestDelegate next, 
            ILogger<RequestTrackingMiddleware> logger) 
            : base(options, next, logger)
        {
        }

        protected override string SanitizeRequestBody(HttpRequest request, string requestBody)
        {
            JObject order = JObject.Parse(requestBody);
            order.Property("clientId").Remove();
            
            return order.ToString();
        }

        protected override string SanitizeResponseBody(HttpResponse response, string responseBody)
        {
            JObject order = JObject.Parse(responseBody);
            order.Property("id").Remove();
            order.Property("clientId").Remove();

            return order.ToString();
        }
    }
}
