using System;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging.Controllers
{
    [ApiController]
    [RequestTracking(404)]
    public class TrackedNotFoundStatusCodeOnClassController : ControllerBase
    {
        public const string Route = "requesttracking/client-errors-notfound/on-class";

        [HttpPost]
        [Route(Route)]
        public IActionResult Post([FromBody] string responseStatusCode)
        {
            return StatusCode(Convert.ToInt32(responseStatusCode), $"response-{Guid.NewGuid()}");
        }
    }
}
