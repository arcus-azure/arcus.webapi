using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging.Controllers
{
    [ApiController]
    public class ExcludeRequestTrackingOnMethodController : ControllerBase
    {
        public const string Route = "requesttracking/skip-onmethod";

        [HttpPost]
        [Route(Route)]
        [ExcludeRequestTracking]
        public IActionResult Post([FromBody] string body)
        {
            return Ok();
        }
    }
}
