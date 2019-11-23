using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
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

        [HttpGet]
        [Route("authz/shared-access-key-querystring")]
        [QueryStringSharedAccessKeyAuthentication(parameterName: "api-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredSharedAccessKeyQuerystring(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}