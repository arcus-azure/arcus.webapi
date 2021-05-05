using System;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class StubbedStatusCodeController : ControllerBase
    {
        public const string PostRoute = "requesttracking/stubbed-statuscode";

        [HttpPost]
        [Route(PostRoute)]
        public IActionResult Post([FromBody] string responseStatusCode)
        {
            return StatusCode(Convert.ToInt32(responseStatusCode), $"response-{Guid.NewGuid()}");
        }
    }
}
