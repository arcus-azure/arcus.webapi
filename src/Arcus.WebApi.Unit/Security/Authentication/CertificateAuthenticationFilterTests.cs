using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication;
using Arcus.WebApi.Unit.Hosting;
using Arcus.WebApi.Unit.Security.Doubles;
using Xunit;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    public class CertificateAuthenticationFilterTests
    {
        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            _testServer.AddFilter(new CertificateAuthenticationFilter(X509Validation.SubjectName, "subject-name"));
            _testServer.SetClientCertificate(SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"));

            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    NoneAuthenticationController.Route);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Arrange
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectName,
            string issuerName,
            bool expected)
        {
            // Arrange
            _testServer.AddFilter(new CertificateAuthenticationFilter(X509Validation.SubjectName, "CN=known-subject"));
            _testServer.AddFilter(new CertificateAuthenticationFilter(X509Validation.IssuerName, "CN=known-issuername"));

            _testServer.SetClientCertificate(
                SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerName, subjectName));

            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    NoneAuthenticationController.Route);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.True(
                        (HttpStatusCode.Unauthorized == response.StatusCode) == expected,
                        $"Response HTTP status code {(expected ? "should" : "shouldn't")} be 'Unauthorized' but was '{response.StatusCode}'");
                }
            }
        }
    }
}
