using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class SabotageController : ControllerBase
    {
        public const string Route = "/api/sabotage";

        [HttpGet]
        [Route(Route)]
        public IActionResult Get()
        {
            throw new SabotageException("Sabotage this request!");
        }
    }
}
