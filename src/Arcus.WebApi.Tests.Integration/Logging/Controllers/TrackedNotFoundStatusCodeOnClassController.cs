using System;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    [RequestTracking(404)]
    public class TrackedNotFoundStatusCodeOnClassController : ControllerBase
    {
        public const string Route = "requesttracking/client-errors-notfound/on-class";

        [HttpPost]
        [Route(Route)]
        public IActionResult Post([FromQuery] int responseStatusCode)
        {
            return StatusCode(responseStatusCode, "response");
        }
    }
}
