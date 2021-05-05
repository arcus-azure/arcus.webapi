using Arcus.WebApi.Security.Authentication.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class AllowAnonymousCertificateController : ControllerBase
    {
        public const string Route = "authz/certificate/anonymous/controller";

        [HttpGet]
        [Route(Route)]
        [CertificateAuthentication]
        public IActionResult Anonymous()
        {
            return Ok();
        }
    }
}
