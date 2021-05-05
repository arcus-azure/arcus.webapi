using Arcus.WebApi.Security.Authentication.Certificates;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Arcus.WebApi.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers
{
    [ApiController]
    public class BypassOnMethodController : ControllerBase
    {
        public const string SharedAccessKeyRoute = "authz/bypass/sharedaccesskey",
                            CertificateRoute = "authz/bypass/certificate",
                            JwtRoute = "authz/bypass/jwt",
                            AllowAnonymousRoute = "authz/bypass/anonymous";

        [HttpGet]
        [Route(SharedAccessKeyRoute)]
        [BypassSharedAccessKeyAuthentication]
        public IActionResult SharedAccessKey()
        {
            return Ok();
        }

        [HttpGet]
        [Route(CertificateRoute)]
        [BypassCertificateAuthentication]
        public IActionResult Certifcate()
        {
            return Ok();
        }

        [HttpGet]
        [Route(JwtRoute)]
        [BypassJwtTokenAuthorization]
        public IActionResult Jwt()
        {
            return Ok();
        }

        [HttpGet]
        [Route(AllowAnonymousRoute)]
        [AllowAnonymous]
        public IActionResult Anonymous()
        {
            return Ok();
        }
    }
}
