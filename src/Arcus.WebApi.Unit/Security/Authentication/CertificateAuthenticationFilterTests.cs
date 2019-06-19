using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication;
using Arcus.WebApi.Unit.Hosting;
using Arcus.WebApi.Unit.Security.Doubles;
using Xunit;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    public class CertificateAuthenticationFilterTests : IDisposable
    {
        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            _testServer.AddFilter(new CertificateAuthenticationFilter(X509ValidationRequirement.SubjectName, "subject-name"));
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithSubject("unrecognized-subject-name"))
            {
                _testServer.SetClientCertificate(clientCertificate);
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
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("thumbprint-noise", true)]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateThumbprintDoesntMatch(
            string thumbprintNoise,
            bool expected)
        {
            // Arrange
            using (X509Certificate2 clientCertificate = SelfSignedCertificate.Create())
            {
                _testServer.AddFilter(new CertificateAuthenticationFilter(X509ValidationRequirement.Thumbprint, clientCertificate.Thumbprint + thumbprintNoise));
                _testServer.SetClientCertificate(clientCertificate);

                using (HttpClient client = _testServer.CreateClient())
                {
                    var request = new HttpRequestMessage(
                        HttpMethod.Get,
                        NoneAuthenticationController.Route);

                    // Act
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        // Arrange
                        Assert.True(
                            (HttpStatusCode.Unauthorized == response.StatusCode) == expected,
                            $"Response HTTP status code {(expected ? "should" : "shouldn't")} be 'Unauthorized' but was '{response.StatusCode}'");
                    }
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
            _testServer.AddFilter(
                new CertificateAuthenticationFilter(
                    (X509ValidationRequirement.SubjectName, "CN=known-subject"),
                    (X509ValidationRequirement.IssuerName, "CN=known-issuername")));

            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerName, subjectName))
            {
                _testServer.SetClientCertificate(clientCertificate);
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _testServer.Dispose();
        }
    }
}
