using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    [ApiController]
    public class NoneAuthenticationController : ControllerBase
    {
        public const string Route = "autzh/none";

        [HttpGet]
        [Route(Route)]
        public Task<IActionResult> NoneControllerAuthentication(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
