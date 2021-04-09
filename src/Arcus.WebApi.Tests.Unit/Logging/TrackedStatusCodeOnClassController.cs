using System.Net;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    [ApiController]
    [RequestTracking(HttpStatusCode.OK)]
    public class TrackedStatusCodeOnClassController : ControllerBase
    {
        public const string Route200Ok = "requesttracking/tracked-statuscode/on-class/200ok",
                            Route202Accepted = "requesttracking/tracked-statuscode/on-class/202accepted";

        [HttpPost]
        [Route(Route200Ok)]
        [RequestTracking(HttpStatusCode.OK)]
        public IActionResult Post200Ok([FromBody] string body)
        {
            return Ok(body.Replace("request", "response"));
        }
        
        [HttpPost]
        [Route(Route202Accepted)]
        [RequestTracking(HttpStatusCode.Accepted)]
        public IActionResult Post([FromBody] string body)
        {
            return Accepted("uri", body.Replace("request", "response"));
        }
    }
}
