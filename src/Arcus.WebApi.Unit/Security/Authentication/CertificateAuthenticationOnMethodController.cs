using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    [ApiController]
    public class CertificateAuthenticationOnMethodController : ControllerBase
    {
        public const string AuthorizedRoute_SubjectName = "authz/certificate-subject",
                            AuthorizedRoute_SubjectAndIssuerName = "authz/certificate-subject-and-issuername";

        public const string SubjectKey = "subject", IssuerKey = "isser", ThumbprintKey = "thumbprint";

        [HttpGet]
        [Route(AuthorizedRoute_SubjectName)]
        [CertificateAuthentication(X509ValidationRequirement.SubjectName, SubjectKey)]
        public Task<IActionResult> TestConfiguredClientCertificateSubjectName(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route(AuthorizedRoute_SubjectAndIssuerName)]
        [CertificateAuthentication(X509ValidationRequirement.SubjectName, SubjectKey)]
        [CertificateAuthentication(X509ValidationRequirement.IssuerName, IssuerKey)]
        [CertificateAuthentication(X509ValidationRequirement.Thumbprint, ThumbprintKey)]
        public Task<IActionResult> TestConfiguredAllRequirementsClientCertificate(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
