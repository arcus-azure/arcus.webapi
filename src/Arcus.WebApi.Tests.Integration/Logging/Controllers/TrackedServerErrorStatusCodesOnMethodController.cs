using System;
using System.Net;
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class TrackedServerErrorStatusCodesOnMethodController : ControllerBase
    {
        public const string RouteWithMinMax = "requesttracking/tracked/server-errors-minmax/on-method",
                            RouteWithFixed = "requesttracking/tracked/server-errors-fixed/on-method",
                            RouteWithCombined = "requesttracking/tracked/server-errors-combined/on-method",
                            RouteWithAll = "requesttracking/tracked-server-errors-all/on-method";

        [HttpPost]
        [Route(RouteWithMinMax)]
        [RequestTracking(500, 599)]
        public IActionResult PostMinMax([FromQuery] int responseStatusCode)
        {
            return StatusCode(responseStatusCode);
        }

        [HttpPost]
        [Route(RouteWithFixed)]
        [RequestTracking(500)]
        public IActionResult PostFixed([FromQuery] int responseStatusCode)
        {
            return StatusCode(responseStatusCode);
        }

        [HttpPost]
        [Route(RouteWithCombined)]
        [RequestTracking(500, 549)]
        [RequestTracking(550, 599)]
        public IActionResult PostCombined([FromQuery] int responseStatusCode)
        {
            return StatusCode(responseStatusCode);
        }

        [HttpPost]
        [Route(RouteWithAll)]
        [RequestTracking(550, 599)]
        [RequestTracking(Exclude.RequestBody)]
        [RequestTracking(HttpStatusCode.InternalServerError)]
        public IActionResult PostAll([FromQuery] int responseStatusCode)
        {
            return StatusCode(responseStatusCode);
        }
        
        private IActionResult StatusCode(int responseStatusCode)
        {
            return StatusCode(responseStatusCode, "response");
        }
    }
}
