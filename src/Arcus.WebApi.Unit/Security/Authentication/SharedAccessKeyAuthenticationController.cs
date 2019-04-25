using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Unit.Security.Authentication 
{
    [ApiController]
    public class SharedAccessKeyAuthenticationController : ControllerBase
    {
        [HttpGet]
        [Route("authz/shared-access-key")]
        [SharedAccessKeyAuthentication(headerName: "x-shared-access-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredSharedAccessKeyHeaderName(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}