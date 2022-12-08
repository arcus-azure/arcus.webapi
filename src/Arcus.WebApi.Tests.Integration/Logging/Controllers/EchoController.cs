using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class EchoController : ControllerBase
    {
        public const string GetPostRoute = "echo";

        [HttpGet]
        [Route(GetPostRoute)]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpPost]
        [Route(GetPostRoute)]
        public IActionResult Post([FromBody] string body)
        {
            return Ok(body);
        }
    }
}
