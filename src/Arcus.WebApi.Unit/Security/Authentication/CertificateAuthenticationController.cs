using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    [ApiController]
    public class CertificateAuthenticationController : ControllerBase
    {
        public const string AuthorizedRoute_SubjectName = "authz/certificate-subject";

        [HttpGet]
        [Route("authz/certificate-subject")]
        [CertificateAuthentication(X509Validation.SubjectName, "subject")]
        public Task<IActionResult> TestHardCodedConfiguredClientCertificateSubjectName(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
