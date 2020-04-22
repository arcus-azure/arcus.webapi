using System;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization;
using Arcus.WebApi.Security.Authorization.Jwt;
using IdentityModel.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer4.Models;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Secret = IdentityServer4.Models.Secret;

namespace Arcus.WebApi.Tests.Unit.Hosting
{
    /// <summary>
    /// Represents a test implementation of a OpenId server to authenticate with in combination with the <see cref="AzureManagedIdentityAuthorizationFilter"/>.
    /// </summary>
    public class TestOpenIdServer : IDisposable
    {
        private readonly IWebHost _host;
        private readonly string _address;

        private static readonly HttpClient HttpClient = new HttpClient();

        private TestOpenIdServer(string address, IWebHost host)
        {
            _address = address;
            _host = host;

            OpenIdAddressConfiguration = $"{address.TrimEnd('/')}/.well-known/openid-configuration";
        }

        /// <summary>
        /// Gets the HTTP endpoint of the OpenId configuration.
        /// </summary>
        public string OpenIdAddressConfiguration { get; }

        /// <summary>
        /// Generates a valid set of validation parameters to use in combination with <see cref="JwtTokenReader"/> to validate the JWT bearer token.
        /// </summary>
        public async Task<TokenValidationParameters> GenerateTokenValidationParametersAsync()
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(OpenIdAddressConfiguration, new OpenIdConnectConfigurationRetriever());
            OpenIdConnectConfiguration configuration = await configurationManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidateLifetime = true
            };
            return validationParameters;
        }

        /// <summary>
        /// Requests a JWT bearer access token to send authorized HTTP requests.
        /// </summary>
        public async Task<string> RequestAccessTokenAsync()
        {
            DiscoveryResponse disco = await HttpClient.GetDiscoveryDocumentAsync(_address);

            var tokenResponse = await HttpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1"
            });

            Assert.False(String.IsNullOrWhiteSpace(tokenResponse.AccessToken), "Access token is blank");
            return tokenResponse.AccessToken;
        }

        /// <summary>
        /// Starts a new OpenId test server on the given <paramref name="address"/>.
        /// </summary>
        /// <param name="outputWriter">The logger to write diagnostic messages during the lifetime of the the OpenId server.</param>
        /// <param name="address">The address on which the server will be available.</param>
        public static async Task<TestOpenIdServer> StartNewAsync(ITestOutputHelper outputWriter, string address = "https://localhost:4000/")
        {
            IWebHost host =
                new WebHostBuilder()
                    .UseUrls(address)
                    .UseKestrel()
                    .UseSerilog()
                    .ConfigureServices(ConfigureServices)
                    .Configure(Configure)
                    .Build();

            Task.Run(async () =>
            {
                try
                {
                    await host.RunAsync();
                }
                catch (Exception exception)
                {
                    outputWriter.WriteLine(exception.Message);
                }
            });
            await WaitUntilAvailableAsync(address);

            return new TestOpenIdServer(address, host);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentityServer()
                    .AddInMemoryIdentityResources(new[] { new IdentityResources.OpenId() })
                    .AddInMemoryApiResources(new[] { new ApiResource("api1", "My API") })
                    .AddInMemoryClients(new[]
                    {
                        new Client
                        {
                            ClientId = "client",
                            AllowedGrantTypes = GrantTypes.ClientCredentials,
                            ClientSecrets =
                            {
                                new Secret("secret".Sha256())
                            },
                            AllowedScopes = { "api1" }
                        }
                    })
                    .AddDeveloperSigningCredential();
        }

        private static void Configure(IApplicationBuilder app)
        {
            app.UseIdentityServer();
        }

        private static async Task WaitUntilAvailableAsync(string address)
        {
            await Policy.TimeoutAsync(TimeSpan.FromSeconds(30))
                        .WrapAsync(Policy.Handle<Exception>()
                                         .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(1)))
                        .ExecuteAsync(async () =>
                        {
                            using (HttpResponseMessage response = await HttpClient.GetAsync(address))
                            {
                            }
                        });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _host.Dispose();
        }
    }
}
