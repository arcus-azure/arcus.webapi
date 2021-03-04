using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Hosting
{
    [ApiController]
    public class EchoController : ControllerBase
    {
        public const string Route = "echo";

        [HttpGet]
        [Route(Route)]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpPost]
        [Route(Route)]
        public IActionResult Post([FromBody] string body)
        {
            return Ok(body);
        }

    }
}
