using Arcus.WebApi.Correlation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Arcus.WebApi.Unit.Correlation
{
    [ApiController]
    public class CorrelationController : ControllerBase
    {
        public const string Route = "correlation";

        private readonly CorrelationInfo _correlationInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationController"/> class.
        /// </summary>
        public CorrelationController(CorrelationInfo correlationInfo)
        {
            _correlationInfo = correlationInfo;
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
