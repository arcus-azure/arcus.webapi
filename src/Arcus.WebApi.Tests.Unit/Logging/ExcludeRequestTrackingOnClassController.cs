using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    [ApiController]
    [ExcludeRequestTracking]
    public class ExcludeRequestTrackingOnClassController : ControllerBase
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