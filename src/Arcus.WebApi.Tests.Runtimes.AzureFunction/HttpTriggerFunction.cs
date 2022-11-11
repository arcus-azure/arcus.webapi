using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.WebApi.Tests.Runtimes.AzureFunction
{
    public class HttpTriggerFunction
    {
        private readonly AzureFunctionsInProcessHttpCorrelation _correlationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTriggerFunction"/> class.
        /// </summary>
        public HttpTriggerFunction(
            AzureFunctionsInProcessHttpCorrelation correlationService)
        {
            _correlationService = correlationService;
        }
        
        [FunctionName("HttpTriggerFunction")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            _correlationService.AddCorrelationResponseHeaders(req.HttpContext);

            CorrelationInfo correlationInfo = _correlationService.GetCorrelationInfo();
            log.LogInformation("Gets the HTTP correlation: [OperationId={OperationId}, TransactionId={TransactionId}]", correlationInfo.OperationId, correlationInfo.TransactionId);

            string json = JsonConvert.SerializeObject(correlationInfo);
            return new OkObjectResult(json);
        }
    }
}
