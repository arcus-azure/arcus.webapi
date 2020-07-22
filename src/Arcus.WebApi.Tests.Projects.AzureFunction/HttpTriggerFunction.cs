using System;
using System.IO;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Correlation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.WebApi.Tests.Projects.AzureFunction
{
    public class HttpTriggerFunction
    {
        private readonly HttpCorrelation _correlationService;
        private readonly ICorrelationInfoAccessor _correlationInfoAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTriggerFunction"/> class.
        /// </summary>
        public HttpTriggerFunction(HttpCorrelation correlationService, ICorrelationInfoAccessor correlationInfoAccessor)
        {
            _correlationService = correlationService;
            _correlationInfoAccessor = correlationInfoAccessor;
        }
        
        [FunctionName("HttpTriggerFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            if (_correlationService.TryHttpCorrelate(out string errorMessage))
            {
                return new BadRequestObjectResult(errorMessage);
            }

            string json = JsonConvert.SerializeObject(_correlationInfoAccessor.GetCorrelationInfo());
            return new OkObjectResult(json);
        }
    }
}
