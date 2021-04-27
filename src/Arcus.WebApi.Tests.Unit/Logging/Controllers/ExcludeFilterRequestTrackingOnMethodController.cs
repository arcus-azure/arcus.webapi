using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging.Controllers
{
    [ApiController]
    public class ExcludeFilterRequestTrackingOnMethodController : ControllerBase
    {
        public const string OnlyExcludeRequestBodyRoute = "requesttracking/on-method/exclude-requestbody",
                            OnlyExcludeResponseBodyRoute = "requesttracking/on-method/exclude-responsebody",
                            ExcludeAllRoute = "requesttracking/on-method/exclude-all";

        public const string ResponsePrefix = "resp-";
        
        [HttpPost]
        [Route(OnlyExcludeRequestBodyRoute)]
        [RequestTracking(Exclude.RequestBody)]
        public IActionResult OnlyExcludeRequestBody([FromBody] string body)
        {
            return Ok(ResponsePrefix + body);
        }

        [HttpPost]
        [Route(OnlyExcludeResponseBodyRoute)]
        [RequestTracking(Exclude.ResponseBody)]
        public IActionResult OnlyExcludeResponseBody([FromBody] string body)
        {
            return Ok(ResponsePrefix + body);
        }
    }
}
