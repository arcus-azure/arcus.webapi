using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication 
{
    [ApiController]
    public class SharedAccessKeyAuthenticationController : ControllerBase
    {
        private const string AuthorizedRoute = "/authz/shared-access-key",
                             AuthorizedRouteHeader = "/authz/shared-access-key-header",
                             AuthorizedRouteQueryString = "/authz/shared-access-key-querystring";

        [HttpGet]
        [Route(AuthorizedRoute)]
        [SharedAccessKeyAuthentication(headerName: "x-shared-access-key", queryParameterName: "api-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredSharedAccessKey(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route(AuthorizedRouteHeader)]
        [SharedAccessKeyAuthentication(headerName: "x-shared-access-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredHeaderSharedAccessKey(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route(AuthorizedRouteQueryString)]
        [SharedAccessKeyAuthentication(queryParameterName: "api-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredQueryStringSharedAccessKey(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}