using Arcus.WebApi.Security.Authentication.Certificates;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers
{
    [ApiController]
    [BypassCertificateAuthentication]
    public class BypassCertificateController : ControllerBase
    {
        public const string BypassOverAuthenticationRoute = "authz/bypass/over/certificate";

        [HttpGet]
        [Route(BypassOverAuthenticationRoute)]
        [CertificateAuthentication]
        public IActionResult BypassOverAuthentiation()
        {
            return Ok();
        }
    }
}
