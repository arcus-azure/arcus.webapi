using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Logging.Controllers
{
    [ApiController]
    [RequestTracking(Exclude.ResponseBody)]
    public class ExcludeFilterUsedWhileExcludedOnClassController : ControllerBase
    {
        public const string Route = "request-tracking/on-method/used-while-also-on-class",
                            ResponsePrefix = "resp-";

        [HttpPost]
        [Route(Route)]
        [RequestTracking(Exclude.RequestBody)]
        public IActionResult BigPost([FromBody] string body)
        {
            return Ok(ResponsePrefix + body);
        }
    }
}
