using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Testing.Logging;
using Arcus.Testing.Security.Providers.InMemory;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.Security.Authentication
{
    [Collection("Integration")]
    public class SharedAccessKeyAuthenticationAttributeTests
    {
        private const string HeaderName = "x-shared-access-key",
                             HeaderNameUpper = "X-SHARED-ACCESS-KEY",
                             SecretName = "custom-access-key-name",
                             AuthorizedRoute = "/authz/shared-access-key",
                             AuthorizedRouteHeader = "/authz/shared-access-key-header",
                             AuthorizedRouteQueryString = "/authz/shared-access-key-querystring",
                             ParameterName = "api-key",
                             ParameterNameUpper = "API-KEY";

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationAttributeTests"/> class.
        /// </summary>
        public SharedAccessKeyAuthenticationAttributeTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithKeyNotFoundException_WhenNoSecretProviderWasRegistered()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new ServerOptions();

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRoute)
                    .WithHeader(HeaderName, secretValue);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ISecretProvider), HeaderName)]
        [InlineData(typeof(ISecretProvider), HeaderNameUpper)]
        [InlineData(typeof(ICachedSecretProvider), HeaderName)]
        [InlineData(typeof(ICachedSecretProvider), HeaderNameUpper)]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithSecretProvider_ShouldNotFailWithUnauthorized(Type secretProviderType, string headerName)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteHeader)
                    .WithHeader(headerName, secretValue);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(HeaderName, "some header value")]
        [InlineData("some.header.name", "some header value")]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized(string headerName, string headerValue)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteHeader)
                    .WithHeader(headerName, headerValue);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenAnyHeaderValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteHeader)
                    .WithHeader(HeaderName, $"{secretValue};second header value other than {nameof(secretValue)}");
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ISecretProvider), ParameterName)]
        [InlineData(typeof(ISecretProvider), ParameterNameUpper)]
        [InlineData(typeof(ICachedSecretProvider), ParameterName)]
        [InlineData(typeof(ICachedSecretProvider), ParameterNameUpper)]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnQueryParameter_RegisteredWIthSecretProvider_ShouldNotFailWithUnauthorized(
            Type secretProviderType, string parameterName)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteQueryString)
                    .WithParameter(parameterName, secretValue);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(ParameterName, "some parameter value")]
        [InlineData("some parameter name", "some parameter value")]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnQueryParameter_ShouldFailWithUnauthorized(string parameterName, string parameterValue)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRouteQueryString)
                    .WithParameter(parameterName, parameterValue);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ISecretProvider))]
        [InlineData(typeof(ICachedSecretProvider))]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnBothHeaderAndQueryParameter_ShouldNotFailWithUnauthorized(Type secretProviderType)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRoute)
                    .WithHeader(HeaderName, secretValue)
                    .WithParameter(ParameterName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData("some header value", "some parameter value")]
        [InlineData("some header value", "secret")]
        [InlineData("secret", "some parameter value")]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnBothHeaderAndQueryParameter_ShouldFailWithUnauthorized(string headerValue, string parameterValue)
        {
            // Arrange
            string secretValue = "secret";
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(SharedAccessKeyAuthenticationController.AuthorizedGetRoute)
                    .WithHeader(HeaderName, headerValue)
                    .WithParameter(ParameterName, parameterValue);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(BypassOnMethodController.SharedAccessKeyRoute)]
        [InlineData(BypassSharedAccessKeyController.BypassOverAuthenticationRoute)]
        [InlineData(AllowAnonymousSharedAccessKeyController.Route)]
        public async Task SharedAccessKeyAuthorizedRoute_WithBypassAttributeOnMethod_SkipsAuthentication(string route)
        {
            // Arrange
            var options = new ServerOptions()
                .ConfigureServices(services => services.AddMvc(opt => opt.Filters.Add(new SharedAccessKeyAuthenticationFilter(HeaderName, queryParameterName: null, SecretName))));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(route);
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpRequest(string route, string headerName = null, string headerValue = null, string parameterName = null, string parameterValue = null)
        {
            return await SendAuthorizedHttpRequest(route, headerName, new[] { headerValue }, parameterName, parameterValue);
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpRequest(string route, string headerName = null, IEnumerable<string> headerValues = null, string parameterName = null, string parameterValue = null)
        {
            string requestUri = parameterName == null ? route : route + $"?{parameterName}={parameterValue}";

            var options = new ServerOptions();
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(requestUri);

                if (headerName != null)
                {
                    request.WithHeader(headerName, String.Join(";", headerValues));
                }

                return await server.SendAsync(request);
            }
        }
    }
}
