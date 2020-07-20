using Arcus.Observability.Correlation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog.Extensions.Hosting;

namespace Arcus.WebApi.Tests.Unit.Correlation
{
    [ApiController]
    public class CorrelationController : ControllerBase
    {
        public const string DefaultRoute = "correlation",
                            SetCorrelationRoute = "correlation/set";

        private readonly ICorrelationInfoAccessor _correlationInfoAccessor;
        private readonly DiagnosticContext _diagnosticContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationController"/> class.
        /// </summary>
        public CorrelationController(ICorrelationInfoAccessor correlationInfoAccessor, DiagnosticContext diagnosticContext)
        {
            _correlationInfoAccessor = correlationInfoAccessor;
            _diagnosticContext = diagnosticContext;
        }

        [HttpGet]
        [Route(DefaultRoute)]
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
