using System.Net.Http;
using Arcus.WebApi.Security.Authentication.Certificates;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.OpenApi.Controllers
{
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        public const string OAuthRoute = "openapi/auth/oauth",
                            SharedAccessKeyRoute = "openapi/auth/sharedaccesskey",
                            CertificateRoute = "openapi/auth/certificate",
                            NoneRoute = "openapi/auth/none";

        [HttpGet]
        [Route(OAuthRoute)]
        [Authorize]
        public IActionResult GetOAuthAuthorized()
        {
            return Ok();
        }

        [HttpGet]
        [Route(CertificateRoute)]
        [CertificateAuthentication]
        public IActionResult GetCertificateAuthorized()
        {
            return Ok();
        }

        [HttpGet]
        [Route(SharedAccessKeyRoute)]
        [SharedAccessKeyAuthentication("secretName", "headerName")]
        public IActionResult GetSharedAccessKeyAuthorized()
        {
            return Ok();
        }

        [HttpGet]
        [Route(NoneRoute)]
        public IActionResult GetNoneAuthorized()
        {
            return Ok();
        }
    }
}
