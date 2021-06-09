using Arcus.Observability.Correlation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog.Extensions.Hosting;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class CorrelationController : ControllerBase
    {
        public const string GetRoute = "correlation",
                            SetCorrelationRoute = "correlation/set";

        private readonly ICorrelationInfoAccessor _correlationInfoAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationController"/> class.
        /// </summary>
        public CorrelationController(ICorrelationInfoAccessor correlationInfoAccessor)
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
        public IActionResult Post([FromHeader(Name = "RequestId")] string operationId, [FromHeader(Name = "X-Transaction-ID")] string transactionId)
        {
            _correlationInfoAccessor.SetCorrelationInfo(new CorrelationInfo(operationId, transactionId));

            string json = JsonConvert.SerializeObject(_correlationInfoAccessor.GetCorrelationInfo());
            return Ok(json);
        }
    }
}
