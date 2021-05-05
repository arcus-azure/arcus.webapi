using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.Certificates;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers
{
    [ApiController]
    public class CertificateAuthenticationOnMethodController : ControllerBase
    {
        public const string AuthorizedGetRoute = "authz/certificate";

        [HttpGet]
        [Route(AuthorizedGetRoute)]
        [CertificateAuthentication]
        public Task<IActionResult> TestCertificateAuthentication(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
