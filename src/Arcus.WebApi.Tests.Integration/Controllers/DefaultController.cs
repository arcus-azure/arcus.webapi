using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Controllers
{
    [ApiController]
    [Route(GetRoute)]
    public class DefaultController : ControllerBase
    {
        public const string GetRoute = "api/v1/default";

        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
