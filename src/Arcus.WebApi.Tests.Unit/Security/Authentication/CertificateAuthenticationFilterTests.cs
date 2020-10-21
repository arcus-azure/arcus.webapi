using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.WebApi.Security.Authentication.Certificates;
using Arcus.WebApi.Tests.Unit.Hosting;
using Arcus.WebApi.Tests.Unit.Security.Doubles;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class CertificateAuthenticationFilterTests : IDisposable
    {
        private readonly TestApiServer _testServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationFilterTests"/> class.
        /// </summary>
        public CertificateAuthenticationFilterTests(ITestOutputHelper outputWriter)
        {
            _testServer = new TestApiServer(outputWriter);
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailWithUnauthorized_WhenClientCertificateSubjectNameDoesntMatch()
        {
            // Arrange
            string subjectKey = "subject", subjectValue = $"subject-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((subjectKey, subjectValue)));
            _testServer.AddService(
                new CertificateAuthenticationValidator(
                        new CertificateAuthenticationConfigBuilder()
                            .WithSubject(X509ValidationLocation.SecretProvider, subjectKey)
                            .Build()));
            
            _testServer.AddFilter(new CertificateAuthenticationFilter());
            
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
                        // Assert
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
                    new CertificateAuthenticationValidator(
                            new CertificateAuthenticationConfigBuilder()
                                .WithThumbprint(X509ValidationLocation.SecretProvider, thumbprintKey)
                                .Build()));
                
                _testServer.AddFilter(new CertificateAuthenticationFilter());

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
                        new CertificateAuthenticationConfigBuilder()
                            .WithSubject(X509ValidationLocation.SecretProvider, subjectKey)
                            .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
                            .Build()));

            _testServer.AddFilter(new CertificateAuthenticationFilter());

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
                    new CertificateAuthenticationConfigBuilder()
                        .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                        .WithIssuer(X509ValidationLocation.Configuration, issuerKey)
                        .Build()));

            _testServer.AddFilter(new CertificateAuthenticationFilter());

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
                    new CertificateAuthenticationConfigBuilder()
                        .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                        .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
                        .Build()));

            _testServer.AddFilter(new CertificateAuthenticationFilter());

            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName(issuerValue, subjectValue))
            {
                _testServer.SetClientCertificate(clientCertificate);
                using (HttpClient client = _testServer.CreateClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, NoneAuthenticationController.Route);

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

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthentication_ShouldFailOnInvalidBase64Format()
        {
            // Arrange
            _testServer.AddService(
                new CertificateAuthenticationValidator(
                    new CertificateAuthenticationConfigBuilder()
                        .Build()));
            
            _testServer.AddFilter(new CertificateAuthenticationFilter());

            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, NoneAuthenticationController.Route);
                request.Headers.Add("X-ARR-ClientCert", "something not even close to an client certificate export");

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithCertificateAuthenticationInHeader_ShouldSucceed()
        {
            // Arrange
            const string subjectKey = "subject", issuerKey = "issuer";
            _testServer.AddConfigKeyValue(subjectKey, "CN=known-subject");
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((issuerKey, "CN=known-issuername")));
            _testServer.AddService(
                new CertificateAuthenticationValidator(
                    new CertificateAuthenticationConfigBuilder()
                        .WithSubject(X509ValidationLocation.Configuration, subjectKey)
                        .WithIssuer(X509ValidationLocation.SecretProvider, issuerKey)
                        .Build()));

            _testServer.AddFilter(new CertificateAuthenticationFilter());

            using (X509Certificate2 clientCertificate = SelfSignedCertificate.CreateWithIssuerAndSubjectName("known-issuername", "known-subject"))
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, NoneAuthenticationController.Route);

                string base64String = Convert.ToBase64String(clientCertificate.Export(X509ContentType.Pkcs12), Base64FormattingOptions.None);
                request.Headers.Add("X-ARR-ClientCert", base64String);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
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
