using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    [ExcludeRequestTracking]
    public class ExcludeFilterIgnoredWhileExcludedOnClassController : ControllerBase
    {
        public const string Route = "requesttracking/on-method/ignored-by-class";
        
        public const string ResponsePrefix = "resp-";
        
        [HttpPost]
        [Route(Route)]
        [RequestTracking(Exclude.RequestBody)]
        public IActionResult BigPost([FromBody] string body)
        {
            return Ok(ResponsePrefix + body);
        }
    }
}
