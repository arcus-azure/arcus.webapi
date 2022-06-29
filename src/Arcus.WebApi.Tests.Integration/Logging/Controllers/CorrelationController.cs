using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class CorrelationController : ControllerBase
    {
        public const string GetRoute = "correlation",
                            SetCorrelationRoute = "correlation/set";

        private readonly IHttpCorrelationInfoAccessor _correlationInfoAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationController"/> class.
        /// </summary>
        public CorrelationController(IHttpCorrelationInfoAccessor correlationInfoAccessor)
        {
            _correlationInfoAccessor = correlationInfoAccessor;
        }

        [HttpGet]
        [Route(GetRoute)]
        public IActionResult Get()
        {
            string json = JsonConvert.SerializeObject(_correlationInfoAccessor.GetCorrelationInfo());
            return Ok(json);
        }

        [HttpPost]
        [Route(SetCorrelationRoute)]
        public IActionResult Post(
            [FromHeader(Name = "X-Operation-ID")] string operationId, 
            [FromHeader(Name = "X-Transaction-ID")] string transactionId,
            [FromHeader(Name = "Request-Id")] string operationParentId)
        {
            _correlationInfoAccessor.SetCorrelationInfo(new CorrelationInfo(operationId, transactionId, operationParentId));

            string json = JsonConvert.SerializeObject(_correlationInfoAccessor.GetCorrelationInfo());
            return Ok(json);
        }
    }
}
