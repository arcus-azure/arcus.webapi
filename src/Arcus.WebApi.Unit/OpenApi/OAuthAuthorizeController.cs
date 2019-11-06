using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Unit.OpenApi
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
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route(NoneAuthorizedRoute)]
        public Task<IActionResult> GetNoneAuthorized(HttpRequestMessage request)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
