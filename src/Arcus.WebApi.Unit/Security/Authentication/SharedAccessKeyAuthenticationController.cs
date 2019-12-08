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
        [SharedAccessKeyAuthentication(headerName: "x-shared-access-key", queryParameterName: "api-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredSharedAccessKey(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route("authz/shared-access-key-header")]
        [SharedAccessKeyAuthentication(headerName: "x-shared-access-key", queryParameterName: null, secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredHeaderSharedAccessKey(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route("authz/shared-access-key-querystring")]
        [SharedAccessKeyAuthentication(headerName: null, queryParameterName: "api-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredQueryStringSharedAccessKey(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}