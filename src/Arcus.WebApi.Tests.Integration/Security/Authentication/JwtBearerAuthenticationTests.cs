using Arcus.Security.Core;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers;

using Bogus;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class JwtBearerAuthenticationTests
    {
        private const string IssuerName = "issuer.contoso.com",
                             AudienceName = "audience.contoso.com";

        private readonly ILogger _logger;

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtBearerAuthenticationTests" /> class.
        /// </summary>
        public JwtBearerAuthenticationTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Theory]
        [InlineData("match-secret-value", "match-secret-value", HttpStatusCode.OK)]
        [InlineData("match-secret-value", "not-match-secret-value", HttpStatusCode.Unauthorized)]
        [InlineData("match-secret-value", null, HttpStatusCode.Unauthorized)]
        public async Task AddJwtBearer_WithCorrectSymmetricKey_Succeeds(string expectedSecretValue, string actualSecretValue, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            const string secretName = "JwtSigningKey";
            var suffix = BogusGenerator.Random.AlphaNumeric(50);
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddSecretStore(stores => stores.AddInMemory(secretName, expectedSecretValue + suffix))
                            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer((jwt, serviceProvider) =>
                            {
                                var secretProvider = serviceProvider.GetRequiredService<ISecretProvider>();
                                string key = secretProvider.GetRawSecretAsync(secretName).GetAwaiter().GetResult();
                                jwt.TokenValidationParameters = CreateTokenValidationParametersForSecret(key);
                            });
                })
                .Configure(app => app.UseAuthentication()
                                     .UseAuthorization());

            string tokenText = CreateBearerTokenFromSecret(actualSecretValue + suffix);
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request =
                    HttpRequestBuilder.Get(AuthorizedController.GetRoute)
                                      .WithHeader("Authorization", $"Bearer {tokenText}");

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(expectedStatusCode, response.StatusCode);
                    AssertPostConfigureJwtOptions(server.ServiceProvider);
                }
            }
        }

        [Fact]
        public async Task AddJwtBearer_WithoutBearerScheme_Fails()
        {
            // Arrange
            const string secretName = "JwtSigningKey";
            string secretValue = BogusGenerator.Random.AlphaNumeric(50);

            var options = new TestApiServerOptions()
                          .ConfigureServices(services =>
                          {
                              services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                                      .AddJwtBearer((jwt, serviceProvider) =>
                                      {
                                          var secretProvider = serviceProvider.GetRequiredService<ISecretProvider>();
                                          string key = secretProvider.GetRawSecretAsync(secretName).GetAwaiter().GetResult();
                                          jwt.TokenValidationParameters = CreateTokenValidationParametersForSecret(key);
                                      });
                          })
                          .Configure(app => app.UseAuthentication()
                                               .UseAuthorization());

            string tokenText = CreateBearerTokenFromSecret(secretValue);
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request =
                    HttpRequestBuilder.Get(AuthorizedController.GetRoute)
                                      .WithHeader("Authorization", tokenText);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    AssertPostConfigureJwtOptions(server.ServiceProvider);
                }
            }
        }

        private static TokenValidationParameters CreateTokenValidationParametersForSecret(string key)
        {
            return new TokenValidationParameters
            {
                ValidIssuer = IssuerName,
                ValidAudience = AudienceName,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };
        }

        private static string CreateBearerTokenFromSecret(string secretValue)
        {
            if (secretValue is null)
            {
                return null;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretValue));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: IssuerName,
                audience: AudienceName,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "Bob")
                },
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials);

            string tokenText = new JwtSecurityTokenHandler().WriteToken(token);
            return tokenText;
        }

        private static void AssertPostConfigureJwtOptions(IServiceProvider provider)
        {
            var jwtOptions = provider.GetRequiredService<IOptions<JwtBearerOptions>>();
            Assert.Equal(jwtOptions.Value.Audience, jwtOptions.Value.TokenValidationParameters.ValidAudience);
        }
    }
}
