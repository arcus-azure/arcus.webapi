using System;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging.Controllers
{
    [ApiController]
    [RequestTracking(400, 499)]
    public class TrackedClientErrorStatusCodesOnClassController : ControllerBase
    {
        public const string Route = "requesttracking/tracked-client-errors/on-class";

        [HttpPost]
        [Route(Route)]
        public IActionResult Post([FromBody] string responseStatusCode)
        {
            return StatusCode(Convert.ToInt32(responseStatusCode), $"response-{Guid.NewGuid()}");
        }
    }
}
