using System.Collections.Generic;
using System.Linq;
using Arcus.WebApi.Correlation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting;

namespace Arcus.WebApi.Tests.Unit.Correlation
{
    [ApiController]
    public class CorrelationController : ControllerBase
    {
        public const string Route = "correlation";

        private readonly HttpCorrelationInfo _correlationInfo;
        private readonly DiagnosticContext _diagnosticContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationController"/> class.
        /// </summary>
        public CorrelationController(HttpCorrelationInfo correlationInfo, DiagnosticContext diagnosticContext)
        {
            _correlationInfo = correlationInfo;
            _diagnosticContext = diagnosticContext;
        }

        [HttpGet]
        [Route(Route)]
        public IActionResult Get()
        {
            string json = JsonConvert.SerializeObject(new { _correlationInfo.TransactionId, _correlationInfo.OperationId });
            return Ok(json);
        }
    }
}
