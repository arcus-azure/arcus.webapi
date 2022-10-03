using System.Net;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Azure.Core.Serialization;
using GuardNet;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Runtimes.AzureFunction.Isolated
{
    public class HttpTriggerFunction
    {
        private readonly IHttpCorrelationInfoAccessor _correlationAccessor;
        private readonly JsonObjectSerializer _serializer;
        private readonly ILogger _logger;

        public HttpTriggerFunction(
            IHttpCorrelationInfoAccessor correlationAccessor,
            JsonObjectSerializer serializer,
            ILoggerFactory loggerFactory)
        {
            Guard.NotNull(correlationAccessor, nameof(correlationAccessor));
            Guard.NotNull(serializer, nameof(serializer));

            _correlationAccessor = correlationAccessor;
            _serializer = serializer;
            _logger = loggerFactory.CreateLogger<HttpTriggerFunction>();
        }

        [Function("HttpTriggerFunction")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            req.ReadFromJsonAsync<CorrelationInfo>(_serializer);

            CorrelationInfo correlationInfo = _correlationAccessor.GetCorrelationInfo();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteAsJsonAsync(correlationInfo, _serializer);

            return response;
        }
    }
}
