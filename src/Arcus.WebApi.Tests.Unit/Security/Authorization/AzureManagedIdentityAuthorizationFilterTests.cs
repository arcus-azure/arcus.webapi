using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization;
using Arcus.WebApi.Security.Authorization.Jwt;
using Arcus.WebApi.Tests.Unit.Hosting;
using Bogus;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    public class AzureManagedIdentityAuthorizationFilterTests
    {
        private readonly Faker _bogusGenerator = new Faker();

        [Fact]
        public async Task GetHealthWithCorrectBearerToken_WithAzureManagedIdentityAuthorization_ReturnsOk()
        {
            // Arrange
            using (var testServer = new TestApiServer())
            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync())
            {
                TokenValidationParameters validationParameters = await testOpenIdServer.GenerateTokenValidationParametersAsync();
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                testServer.AddFilter(new AzureManagedIdentityAuthorizationFilter(reader));

                using (HttpClient client = testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = await testOpenIdServer.RequestAccessTokenAsync();
                    request.Headers.Add(AzureManagedIdentityAuthorizationFilter.DefaultHeaderName, accessToken);

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
            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync())
            {
                TokenValidationParameters validationParameters = await testOpenIdServer.GenerateTokenValidationParametersAsync();
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                testServer.AddFilter(new AzureManagedIdentityAuthorizationFilter(reader));

                using (HttpClient client = testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = $"Bearer {_bogusGenerator.Random.AlphaNumeric(10)}.{_bogusGenerator.Random.AlphaNumeric(50)}.{_bogusGenerator.Random.AlphaNumeric(40)}";
                    request.Headers.Add(AzureManagedIdentityAuthorizationFilter.DefaultHeaderName, accessToken);

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
            using (var testServer = new TestApiServer())
            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync())
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true
                };
                var reader = new JwtTokenReader(validationParameters, testOpenIdServer.OpenIdAddressConfiguration);
                testServer.AddFilter(new AzureManagedIdentityAuthorizationFilter(reader));

                using (HttpClient client = testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = await testOpenIdServer.RequestAccessTokenAsync();
                    request.Headers.Add(AzureManagedIdentityAuthorizationFilter.DefaultHeaderName, accessToken);

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
        public async Task GetHealthWithCorrectBearerToken_WithAzureManagedIdentityAuthorizationWithoutOpenIdServer_ReturnsInternalServerError()
        {
            // Arrange
            using (var testServer = new TestApiServer())
            using (var testOpenIdServer = await TestOpenIdServer.StartNewAsync())
            {
                TokenValidationParameters validationParameters = await testOpenIdServer.GenerateTokenValidationParametersAsync();
                var unavailableOpenIdServer = "http://localhost:6000";
                var reader = new JwtTokenReader(validationParameters, unavailableOpenIdServer);
                testServer.AddFilter(new AzureManagedIdentityAuthorizationFilter(reader));

                using (HttpClient client = testServer.CreateClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, HealthController.Route))
                {
                    string accessToken = await testOpenIdServer.RequestAccessTokenAsync();
                    request.Headers.Add(AzureManagedIdentityAuthorizationFilter.DefaultHeaderName, accessToken);

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