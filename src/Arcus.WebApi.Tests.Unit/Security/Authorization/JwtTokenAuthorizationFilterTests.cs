using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization;
using Arcus.WebApi.Security.Authorization.Jwt;
using Arcus.WebApi.Tests.Unit.Hosting;
using Arcus.WebApi.Tests.Unit.Security.Extension;
using Bogus;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    public class JwtTokenAuthorizationFilterTests : IDisposable
    {
        private readonly TestApiServer _testServer;
        private readonly ITestOutputHelper _outputWriter;
        private readonly Faker _bogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationFilterTests"/> class.
        /// </summary>
        public JwtTokenAuthorizationFilterTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
            _testServer = new TestApiServer(_outputWriter);
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            string issuer = GenerateRandomUri();
            string authority = GenerateRandomUri();
            string privateKey = GenerateRandomPrivateKey();

            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_outputWriter))
            {
                TokenValidationParameters tokenValidationParameters = testOpenIdServer.GenerateTokenValidationParametersWithValidAudience(issuer, authority, privateKey);
                var reader = new JwtTokenReader(tokenValidationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                _testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader));

                using (HttpClient client = _testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, daysValid: 7);
                    request.Headers.Add(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                    // Act
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithNullReaderAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            _testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.AddJwtTokenReader(serviceProvider => null)));

            using (HttpClient client = _testServer.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
            {
                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithLazyAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            string issuer = GenerateRandomUri();
            string authority = GenerateRandomUri();
            string privateKey = GenerateRandomPrivateKey();

            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_outputWriter))
            {
                TokenValidationParameters validationParameters = testOpenIdServer.GenerateTokenValidationParametersWithValidAudience(issuer, authority, privateKey);
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                _testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.AddJwtTokenReader(serviceProvider => reader)));

                using (HttpClient client = _testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, daysValid: 7);
                    request.Headers.Add(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                    // Act
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithInjectedAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            _testServer.AddFilter(
                filters => filters.AddJwtTokenAuthorization(
                    options => options.AddJwtTokenReader<IgnoredJwtTokenReader>()));

            using (HttpClient client = _testServer.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
            {
                var accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}.{_bogusGenerator.Random.AlphaNumeric(40)}";
                request.Headers.Add(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetHealthWithIncorrectBearerToken_WithAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            string issuer = GenerateRandomUri();
            string authority = GenerateRandomUri();
            string privateKey = GenerateRandomPrivateKey();

            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_outputWriter))
            {
                TokenValidationParameters validationParameters = testOpenIdServer.GenerateTokenValidationParametersWithValidAudience(issuer, authority, privateKey);
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                _testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader));

                using (HttpClient client = _testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    var accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}.{_bogusGenerator.Random.AlphaNumeric(40)}";
                    request.Headers.Add(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                    // Act
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task GetHealthWithIncorrectBase64BearerToken_WithAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            _testServer.AddFilter(filters => filters.AddJwtTokenAuthorization());

            using (HttpClient client = _testServer.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
            {
                string accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}";
                request.Headers.Add(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithIncorrectAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            string issuer = GenerateRandomUri();
            string authority = GenerateRandomUri();
            string privateKey = GenerateRandomPrivateKey();

            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_outputWriter))
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };

                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                _testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader));

                using (HttpClient client = _testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, daysValid: 7);
                    request.Headers.Add(JwtTokenAuthorizationOptions.DefaultHeaderName, accessToken);

                    // Act
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task GetHealthWithoutBearerToken_WithIncorrectAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_outputWriter))
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                _testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader));

                using (HttpClient client = _testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    // Act
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        // Assert
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            }
        }

        private static string GenerateRandomUri()
        {
            return $"http://{Util.GetRandomString(length: 10).ToLower()}.com";
        }

        private static string GenerateRandomPrivateKey()
        {
            using (RSA rsa = new RSACryptoServiceProvider(dwKeySize: 512))
            {
                string privateKey = rsa.ToCustomXmlString(includePrivateParameters: true);
                return privateKey;
            }

            
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _testServer?.Dispose();
        }
    }
}