using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Interfaces;
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
            string subjectKey = "subject", subjectValue = $"subject-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((subjectKey, subjectValue)));
            _testServer.AddService(
                new CertificateAuthenticationValidator()
                    .AddRequirementLocation(X509ValidationRequirement.SubjectName, X509ValidationLocation.SecretProvider));
            
            _testServer.AddFilter(new CertificateAuthenticationFilter(X509ValidationRequirement.SubjectName, subjectKey));
            
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
                const string thumbprintKey = "thumbprint";
                
                _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((thumbprintKey, clientCertificate.Thumbprint + thumbprintNoise)));
                _testServer.AddService(
                    new CertificateAuthenticationValidator()
                        .AddRequirementLocation(X509ValidationRequirement.Thumbprint, X509ValidationLocation.SecretProvider));
                
                _testServer.AddFilter(new CertificateAuthenticationFilter(X509ValidationRequirement.Thumbprint, thumbprintKey));

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
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaSecretProvider_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            _testServer.AddService<ISecretProvider>(
                new InMemorySecretProvider(
                    (subjectKey, "CN=known-subject"),
                    (issuerKey, "CN=known-issuername")));

            _testServer.AddService(
                new CertificateAuthenticationValidator(
                        new Dictionary<X509ValidationRequirement, X509ValidationLocation>
                        {
                            [X509ValidationRequirement.SubjectName] = X509ValidationLocation.SecretProvider,
                            [X509ValidationRequirement.IssuerName] = X509ValidationLocation.SecretProvider,
                        }));

            _testServer.AddFilter(
                new CertificateAuthenticationFilter(
                    new Dictionary<X509ValidationRequirement, string>
                    {
                        [X509ValidationRequirement.SubjectName] = subjectKey,
                        [X509ValidationRequirement.IssuerName] = issuerKey
                    }
                ));

            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
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

        [Theory]
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaConfiguration_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            _testServer.AddConfigKeyValue(subjectKey, "CN=known-subject");
            _testServer.AddConfigKeyValue(issuerKey, "CN=known-issuername");

            _testServer.AddService(
                new CertificateAuthenticationValidator(
                        new Dictionary<X509ValidationRequirement, X509ValidationLocation>
                        {
                            [X509ValidationRequirement.SubjectName] = X509ValidationLocation.Configuration,
                            [X509ValidationRequirement.IssuerName] = X509ValidationLocation.Configuration,
                        }));

            _testServer.AddFilter(
                new CertificateAuthenticationFilter(
                    new Dictionary<X509ValidationRequirement, string>
                    {
                        [X509ValidationRequirement.SubjectName] = subjectKey,
                        [X509ValidationRequirement.IssuerName] = issuerKey
                    }
                ));

            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
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

        [Theory]
        [InlineData("known-subject", "known-issuername", false)]
        [InlineData("unrecognizedSubjectName", "known-issuername", true)]
        [InlineData("known-subject", "unrecognizedIssuerName", true)]
        [InlineData("unrecognizedSubjectName", "unrecognizedIssuerName", true)]
        public async Task AuthorizedRoute_WithCertificateAuthenticationViaConfigurationAndSecretProvider_ShouldFailWithUnauthorized_WhenAnyClientCertificateValidationDoesntSucceeds(
            string subjectValue,
            string issuerValue,
            bool expected)
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            _testServer.AddConfigKeyValue(subjectKey, "CN=known-subject");
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((issuerKey, "CN=known-issuername")));
            _testServer.AddService(
                new CertificateAuthenticationValidator(
                        new Dictionary<X509ValidationRequirement, X509ValidationLocation>
                        {
                            [X509ValidationRequirement.SubjectName] = X509ValidationLocation.Configuration,
                            [X509ValidationRequirement.IssuerName] = X509ValidationLocation.SecretProvider,
                        }));

            _testServer.AddFilter(
                new CertificateAuthenticationFilter(
                    new Dictionary<X509ValidationRequirement, string>
                    {
                        [X509ValidationRequirement.SubjectName] = subjectKey,
                        [X509ValidationRequirement.IssuerName] = issuerKey
                    }
                ));

            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
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
