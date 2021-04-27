using System;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging.Controllers
{
    [ApiController]
    public class StubbedStatusCodeController : ControllerBase
    {
        public const string Route = "requesttracking/stubbed-statuscode";

        [HttpPost]
        [Route(Route)]
        public IActionResult Post([FromBody] string responseStatusCode)
        {
            return StatusCode(Convert.ToInt32(responseStatusCode), $"response-{Guid.NewGuid()}");
        }
    }
}
