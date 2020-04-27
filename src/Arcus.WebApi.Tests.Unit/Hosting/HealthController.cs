using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Hosting
{
    [Route(Route)]
    [ApiController]
    public class HealthController : ControllerBase
    {
        public const string Route = "/api/v1/health";

        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
