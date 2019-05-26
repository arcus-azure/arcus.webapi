using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Unit.Hosting;
using Arcus.WebApi.Unit.Security.Doubles;
using Xunit;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    public class CertificateAuthenticationAttributeTests
    {
        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            _testServer.SetClientCertificate(StubCertificate.CreateWithSubject("unrecognized-subject-name"));

            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    CertificateAuthenticationController.AuthorizedRoute_SubjectName);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Arrange
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
    }
}
