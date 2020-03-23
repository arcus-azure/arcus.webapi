using Arcus.Observability.Correlation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog.Extensions.Hosting;

namespace Arcus.WebApi.Unit.Correlation
{
    [ApiController]
    public class CorrelationController : ControllerBase
    {
        public const string Route = "correlation";

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
        [Route(Route)]
        public IActionResult Get()
        {
            string json = JsonConvert.SerializeObject(_correlationInfoAccessor.CorrelationInfo);
            return Ok(json);
        }
    }
}
