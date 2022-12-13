using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class RequestOperationNameController : ControllerBase
    {
        public static string GetPostRouteWithRouteParameters(int deviceId)
        {
            return $"devices/{deviceId}/echo";
        }

        [HttpGet]
        [Route("devices/{deviceId}/echo")]
        public IActionResult GetDevice([FromRoute] int deviceId)
        {
            return Ok();
        }

        [HttpPost]
        [Route("devices/{deviceId}/echo")]
        public IActionResult PostDevice([FromRoute] int deviceId, [FromBody] string body)
        {
            return Ok(body);
        }
    }
}
