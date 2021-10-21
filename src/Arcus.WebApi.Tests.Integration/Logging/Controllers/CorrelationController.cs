using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
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

        [HttpGet]
        [Route(GetRoute)]
        public IActionResult Get([FromServices] IHttpCorrelationInfoAccessor accessor)
        {
            string json = JsonConvert.SerializeObject(accessor.GetCorrelationInfo());
            return Ok(json);
        }

        [HttpPost]
        [Route(SetCorrelationRoute)]
        public IActionResult Post(
            [FromHeader(Name = "RequestId")] string operationId, 
            [FromHeader(Name = "X-Transaction-ID")] string transactionId,
            [FromServices] IHttpCorrelationInfoAccessor accessor)
        {
            accessor.SetCorrelationInfo(new CorrelationInfo(operationId, transactionId));

            string json = JsonConvert.SerializeObject(accessor.GetCorrelationInfo());
            return Ok(json);
        }
    }
}
