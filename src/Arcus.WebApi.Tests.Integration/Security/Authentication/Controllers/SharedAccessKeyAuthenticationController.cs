using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers 
{
    [ApiController]
    public class SharedAccessKeyAuthenticationController : ControllerBase
    {
        public const string AuthorizedGetRoute = "/authz/shared-access-key",
                            AuthorizedGetRouteHeader = "/authz/shared-access-key-header",
                            AuthorizedGetRouteQueryString = "/authz/shared-access-key-querystring";

        [HttpGet]
        [Route(AuthorizedGetRoute)]
        [SharedAccessKeyAuthentication(headerName: "x-shared-access-key", queryParameterName: "api-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredSharedAccessKey()
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route(AuthorizedGetRouteHeader)]
        [SharedAccessKeyAuthentication(headerName: "x-shared-access-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredHeaderSharedAccessKey(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route(AuthorizedGetRouteQueryString)]
        [SharedAccessKeyAuthentication(queryParameterName: "api-key", secretName: "custom-access-key-name")]
        public Task<IActionResult> TestHardCodedConfiguredQueryStringSharedAccessKey(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}