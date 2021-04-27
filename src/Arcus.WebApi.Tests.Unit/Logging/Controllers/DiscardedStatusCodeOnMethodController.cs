using System;
using System.Net;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging.Controllers
{
    [ApiController]
    public class DiscardedStatusCodeOnMethodController : ControllerBase
    {
        public const string Route = "requesttracking/disgarded-statuscode/on-method";

        [HttpPost]
        [Route(Route)]
        [RequestTracking(HttpStatusCode.OK)]
        public IActionResult Post([FromBody] string responseStatusCode)
        {
            return StatusCode(Convert.ToInt32(responseStatusCode), $"response-{Guid.NewGuid()}");
        }
    }
}
