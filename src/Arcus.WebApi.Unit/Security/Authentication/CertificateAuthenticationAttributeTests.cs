using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Unit.Hosting;
using Arcus.WebApi.Unit.Security.Doubles;
using Xunit;
using static Arcus.WebApi.Unit.Security.Authentication.CertificateAuthenticationController;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    public class CertificateAuthenticationAttributeTests
    {
        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            _testServer.SetClientCertificate(SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"));

            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    AuthorizedRoute_SubjectName);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Arrange
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(ExpectedSubjectName, ExpectedIssuerName, false)]
        [InlineData("unrecognizedSubjectName", ExpectedIssuerName, true)]
        [InlineData(ExpectedSubjectName, "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectName,
            string issuerName,
            bool expected)
        {
            // Arrange
            _testServer.SetClientCertificate(
                SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerName, subjectName));

            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    AuthorizedRoute_SubjectAndIssuerName);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(
                        HttpStatusCode.Unauthorized == response.StatusCode,
                        expected);
                }
            }
        }
    }
}
