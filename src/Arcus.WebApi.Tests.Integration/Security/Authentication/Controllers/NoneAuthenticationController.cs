using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers
{
    [ApiController]
    public class NoneAuthenticationController : ControllerBase
    {
        public const string GetRoute = "autzh/none";

        [HttpGet]
        [Route(GetRoute)]
        public IActionResult NoneControllerAuthentication()
        {
            return Ok();
        }
    }
}
