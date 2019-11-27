using Arcus.WebApi.Correlation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Arcus.WebApi.Unit.Correlation
{
    [ApiController]
    public class CorrelationController : ControllerBase
    {
        public const string Route = "correlation";

        private readonly ICorrelationAccessor _accessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationController"/> class.
        /// </summary>
        public CorrelationController(ICorrelationAccessor accessor)
        {
            _accessor = accessor;
        }

        [HttpGet]
        [Route(Route)]
        public IActionResult Get()
        {
            string json = JsonConvert.SerializeObject(new { _accessor.CorrelationId, _accessor.RequestId });
            return Ok(json);
        }
    }
}
