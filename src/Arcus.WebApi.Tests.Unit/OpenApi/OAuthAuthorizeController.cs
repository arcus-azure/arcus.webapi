using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.OpenApi
{
    [ApiController]
    public class OAuthAuthorizeController : ControllerBase
    {
        public const string AuthorizedRoute = "oauth/authorize",
                            NoneAuthorizedRoute = "oauth/none";

        [HttpGet]
        [Route(AuthorizedRoute)]
        [Authorize]
        public IActionResult GetAuthorized(HttpRequestMessage request)
        {
            return Ok();
        }

        [HttpGet]
        [Route(NoneAuthorizedRoute)]
        public IActionResult GetNoneAuthorized(HttpRequestMessage request)
        {
            return Ok();
        }
    }
}
