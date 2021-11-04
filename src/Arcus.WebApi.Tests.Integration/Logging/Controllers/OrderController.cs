using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class OrderController : ControllerBase
    {
        public const string PostRoute = "order";

        [HttpPost]
        [Route(PostRoute)]
        public IActionResult Post([FromBody] Order order)
        {
            return Ok(order);
        }
    }
}
