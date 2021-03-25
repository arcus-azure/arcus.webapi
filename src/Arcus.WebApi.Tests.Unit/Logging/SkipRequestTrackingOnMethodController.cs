using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    [ApiController]
    public class SkipRequestTrackingOnMethodController : ControllerBase
    {
        public const string Route = "requesttracking/skip-onmethod";

        [HttpPost]
        [Route(Route)]
        [SkipRequestTracking]
        public IActionResult Post([FromBody] string body)
        {
            return Ok();
        }
    }
}
