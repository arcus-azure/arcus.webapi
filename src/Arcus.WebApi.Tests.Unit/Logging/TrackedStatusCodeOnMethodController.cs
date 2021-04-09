using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    [ApiController]
    public class TrackedStatusCodeOnMethodController : ControllerBase
    {
        public const string Route200Ok = "requesttracking/tracked-statuscode/200ok",
                            Route201Created = "requesttracking/tracked-statuscode/201created",
                            Route202Accepted = "requesttracking/tracked-statuscode/202accepted";

        [HttpPost]
        [Route(Route200Ok)]
        [RequestTracking(HttpStatusCode.OK)]
        public IActionResult PostOk([FromBody] string body)
        {
            return Ok(body.Replace("request", "response"));
        }

        [HttpPost]
        [Route(Route201Created)]
        [RequestTracking(HttpStatusCode.OK, HttpStatusCode.Created)]
        public IActionResult PostCreated([FromBody] string body)
        {
            return Created("uri", body.Replace("request", "response"));
        }

        [HttpPost]
        [Route(Route202Accepted)]
        [RequestTracking(HttpStatusCode.OK)]
        [RequestTracking(HttpStatusCode.Created)]
        [RequestTracking(HttpStatusCode.Accepted)]
        public IActionResult PostAccepted([FromBody] string body)
        {
            return Accepted("uri", body.Replace("request", "response"));
        }
    }
}
