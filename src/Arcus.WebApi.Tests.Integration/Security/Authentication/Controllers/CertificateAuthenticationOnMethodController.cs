using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.Certificates;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers
{
    [ApiController]
    public class CertificateAuthenticationOnMethodController : ControllerBase
    {
        public const string AuthorizedGetRoute = "authz/certificate",
                            AuthorizedGetRouteEmitSecurityEvents = "authz/certificate/emit-security-events";

        [HttpGet]
        [Route(AuthorizedGetRoute)]
        [CertificateAuthentication]
        public IActionResult TestCertificateAuthentication()
        {
            return Ok();
        }

        [HttpGet]
        [Route(AuthorizedGetRouteEmitSecurityEvents)]
        [CertificateAuthentication(EmitSecurityEvents = true)]
        public IActionResult TestEmitSecurityEvents()
        {
            return Ok();
        }
    }
}
