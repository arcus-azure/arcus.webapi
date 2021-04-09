using System;
using System.Net;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    [ApiController]
    [RequestTracking(HttpStatusCode.OK)]
    public class DiscardedStatusCodeOnClassController : ControllerBase
    {
        public const string Route = "requesttracking/disgarded-statuscode/on-class";

        [HttpPost]
        [Route(Route)]
        public IActionResult Post([FromBody] string responseStatusCode)
        {
            return StatusCode(Convert.ToInt32(responseStatusCode), $"response-{Guid.NewGuid()}");
        }
    }
}
