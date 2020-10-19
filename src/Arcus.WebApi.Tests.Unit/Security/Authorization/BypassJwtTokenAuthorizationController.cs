using Arcus.WebApi.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    [ApiController]
    [BypassJwtTokenAuthorization]
    public class BypassJwtTokenAuthorizationController : ControllerBase
    {
        public const string BypassOverAuthorizationRoute = "authz/bypass/over/jwt";

        [HttpGet]
        [Route(BypassOverAuthorizationRoute)]
        public IActionResult BypassOverAuthorization()
        {
            return Ok();
        }
    }
}
