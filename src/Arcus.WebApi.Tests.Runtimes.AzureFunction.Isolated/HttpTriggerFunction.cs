using System.Net;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Runtimes.AzureFunction.Isolated
{
    public class HttpTriggerFunction
    {
        private readonly IHttpCorrelationInfoAccessor _correlationAccessor;
        private readonly ILogger _logger;

        public HttpTriggerFunction(
            IHttpCorrelationInfoAccessor correlationAccessor,
            ILoggerFactory loggerFactory)
        {
            _correlationAccessor = correlationAccessor;
            _logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        }

        [Function("HttpTriggerFunction")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            CorrelationInfo correlationInfo = _correlationAccessor.GetCorrelationInfo();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteAsJsonAsync(correlationInfo);

            return response;
        }
    }
}
