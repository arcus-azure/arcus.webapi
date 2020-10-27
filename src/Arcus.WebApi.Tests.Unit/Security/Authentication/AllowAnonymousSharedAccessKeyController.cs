using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    [ApiController]
    [AllowAnonymous]
    public class AllowAnonymousSharedAccessKeyController : ControllerBase
    {
        public const string Route = "authz/sharedaccesskey/anonymous/controller";

        [HttpGet]
        [Route(Route)]
        [SharedAccessKeyAuthentication("MySecret", "MyHeader")]
        public IActionResult AllowAnymous()
        {
            return Ok();
        }
    }
}
