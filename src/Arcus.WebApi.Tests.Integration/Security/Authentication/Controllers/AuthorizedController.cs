using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers
{
    [ApiController]
    public class AuthorizedController : ControllerBase
    {
        public const string GetRoute = "/get-authorized";

        [HttpGet]
        [Route(GetRoute)]
        [Authorize]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
