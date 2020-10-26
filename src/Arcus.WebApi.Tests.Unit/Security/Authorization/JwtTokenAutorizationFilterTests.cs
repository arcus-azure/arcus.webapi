using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization;
using Arcus.WebApi.Security.Authorization.Jwt;
using Arcus.WebApi.Tests.Unit.Hosting;
using Arcus.WebApi.Tests.Unit.Security.Extension;
using Bogus;
using IdentityModel;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    public class JwtTokenAutorizationFilterTests
    {
        private readonly ITestOutputHelper _outputWriter;
        private readonly Faker _bogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAutorizationFilterTests"/> class.
        /// </summary>
        public JwtTokenAutorizationFilterTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            using (var testServer = new TestApiServer())
            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_outputWriter))
            {
                TokenValidationParameters tokenValidationParameters = testOpenIdServer.GenerateTokenValidationParametersWithValidAudience(issuer, authority, privateKey);
                var reader = new JwtTokenReader(tokenValidationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader));

                using (HttpClient client = testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, 7);
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
        public async Task GetHealthWithCorrectBearerToken_WithAzureManagedIdentityAuthorizationAndCustomClaims_ReturnsOk()
        {
            // Arrange
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            using (var testServer = new TestApiServer())
            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_outputWriter))
            {
                TokenValidationParameters tokenValidationParameters = testOpenIdServer.GenerateTokenValidationParametersWithValidAudience(issuer, authority, privateKey);
                var reader = new JwtTokenReader(tokenValidationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                Dictionary<string, string> claimCheck = new Dictionary<string, string> {{ JwtClaimTypes.Audience, Guid.NewGuid().ToString() } };
                testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader, claimCheck));

                using (HttpClient client = testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, 7, claimCheck);
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
        public async Task GetHealthWithIncorrectBearerToken_WithAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            using (var testServer = new TestApiServer())
            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync(_outputWriter))
            {
                TokenValidationParameters validationParameters = await testOpenIdServer.GenerateTokenValidationParametersAsync();
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader));

                using (HttpClient client = testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}.{_bogusGenerator.Random.AlphaNumeric(40)}";
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
        public async Task GetHealthWithCorrectBearerToken_WithIncorrectAzureManagedIdentityAuthorization_ReturnsUnauthorized()
        {
            // Arrange
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            using (var testServer = new TestApiServer())
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
                testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader));

                using (HttpClient client = testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = testOpenIdServer.RequestSecretToken(issuer, authority, privateKey, 7);
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
            using (var testServer = new TestApiServer())
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
                testServer.AddFilter(filters => filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = reader));

                using (HttpClient client = testServer.CreateClient())
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
    }
}