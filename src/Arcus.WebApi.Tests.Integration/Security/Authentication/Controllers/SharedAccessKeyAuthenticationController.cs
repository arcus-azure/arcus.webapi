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
                            AuthorizedGetRouteQueryString = "/authz/shared-access-key-querystring",
                            AuthorizedGetRouteEmitSecurityEvents = "/authz/shared-access-key/emit-security-events";
        
        public const string HeaderName = "x-shared-access-key",
                            ParameterName = "api-key",
                            SecretName = "custom-access-key-name";

        [HttpGet]
        [Route(AuthorizedGetRoute)]
        [SharedAccessKeyAuthentication(headerName: HeaderName, queryParameterName: ParameterName, secretName: SecretName)]
        public IActionResult TestHardCodedConfiguredSharedAccessKey()
        {
            return Ok();
        }

        [HttpGet]
        [Route(AuthorizedGetRouteHeader)]
        [SharedAccessKeyAuthentication(headerName: HeaderName, secretName: SecretName)]
        public IActionResult TestHardCodedConfiguredHeaderSharedAccessKey()
        {
            return Ok();
        }

        [HttpGet]
        [Route(AuthorizedGetRouteQueryString)]
        [SharedAccessKeyAuthentication(queryParameterName: ParameterName, secretName: SecretName)]
        public IActionResult TestHardCodedConfiguredQueryStringSharedAccessKey()
        {
            return Ok();
        }

        [HttpGet]
        [Route(AuthorizedGetRouteEmitSecurityEvents)]
        [SharedAccessKeyAuthentication(headerName: HeaderName, secretName: SecretName, EmitSecurityEvents = true)]
        public IActionResult TestEmitSecurityEvents()
        {
            return Ok();
        }
    }
}