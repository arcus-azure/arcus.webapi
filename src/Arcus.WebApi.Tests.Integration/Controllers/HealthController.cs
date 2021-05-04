using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Controllers
{
    [ApiController]
    [Route(GetRoute)]
    public class HealthController : ControllerBase
    {
        public const string GetRoute = "api/v1/health";

        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
