using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Correlation;
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
        private readonly HttpCorrelation _correlationService;
        private readonly ILogger<HttpTriggerFunction> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTriggerFunction"/> class.
        /// </summary>
        public HttpTriggerFunction(HttpCorrelation correlationService, ILogger<HttpTriggerFunction> logger)
        {
            _correlationService = correlationService;
            _logger = logger;
        }
        
        [FunctionName("HttpTriggerFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            if (_correlationService.TryHttpCorrelate(out string errorMessage))
            {
                CorrelationInfo correlationInfo = _correlationService.GetCorrelationInfo();
                _logger.LogInformation(
                    "Gets the HTTP correlation: [OperationId={OperationId}, TransactionId={TransactionId}]",
                    correlationInfo.OperationId,
                    correlationInfo.TransactionId);

                string json = JsonConvert.SerializeObject(correlationInfo);
                return new OkObjectResult(json);
            }

            return new BadRequestObjectResult(errorMessage);
        }
    }
}
