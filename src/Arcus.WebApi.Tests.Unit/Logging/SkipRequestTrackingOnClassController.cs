using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    [ApiController]
    [SkipRequestTracking]
    public class SkipRequestTrackingOnClassController : ControllerBase
    {
        public const string Route = "requesttracking/skip-onclass";

        [HttpPost]
        [Route(Route)]
        public IActionResult Post([FromBody] string body)
        {
            return Ok();
        }
    }
}