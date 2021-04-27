using System;
using System.Net;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging.Controllers
{
    [ApiController]
    [RequestTracking(HttpStatusCode.NotFound)]
    [RequestTracking(450, 499)]
    public class TrackedNotFoundAndClientErrorsSubsetStatusCodesOnClassController : ControllerBase
    {
        public const string Route = "requesttracking/tracked-notfound-and-subset-clienterrors/on-class";

        [HttpPost]
        [Route(Route)]
        public IActionResult Post([FromBody] string responseStatusCode)
        {
            return StatusCode(Convert.ToInt32(responseStatusCode), $"response-{Guid.NewGuid()}");
        }
    }
}
