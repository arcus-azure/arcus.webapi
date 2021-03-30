using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging
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
        [ExcludeRequestTracking(ExcludeFilter.ExcludeRequestBody)]
        public IActionResult OnlyExcludeRequestBody([FromBody] string body)
        {
            return Ok(ResponsePrefix + body);
        }

        [HttpPost]
        [Route(OnlyExcludeResponseBodyRoute)]
        [ExcludeRequestTracking(ExcludeFilter.ExcludeResponseBody)]
        public IActionResult OnlyExcludeResponseBody([FromBody] string body)
        {
            return Ok(ResponsePrefix + body);
        }
        
        [HttpPost]
        [Route(ExcludeAllRoute)]
        [ExcludeRequestTracking(ExcludeFilter.ExcludeRoute)]
        public IActionResult ExcludeAll([FromBody] string body)
        {
            return Ok(ResponsePrefix + body);
        }
    }
}
