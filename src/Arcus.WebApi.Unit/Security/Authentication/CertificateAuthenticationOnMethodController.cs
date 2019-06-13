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

        public const string ExpectedSubjectName = "CN=subject",
                            ExpectedIssuerName = "CN=issername";

        [HttpGet]
        [Route(AuthorizedRoute_SubjectName)]
        [CertificateAuthentication(X509ValidationRequirement.SubjectName, ExpectedSubjectName)]
        public Task<IActionResult> TestHardCodedConfiguredClientCertificateSubjectName(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route(AuthorizedRoute_SubjectAndIssuerName)]
        [CertificateAuthentication(X509ValidationRequirement.SubjectName, ExpectedSubjectName)]
        [CertificateAuthentication(X509ValidationRequirement.IssuerName, ExpectedIssuerName)]
        public Task<IActionResult> TestHardCodedConfiguredClientCertificateSubjectAndIssuerName(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
