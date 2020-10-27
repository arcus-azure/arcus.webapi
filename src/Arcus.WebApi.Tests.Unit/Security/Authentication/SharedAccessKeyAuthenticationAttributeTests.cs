using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Arcus.WebApi.Tests.Unit.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class SharedAccessKeyAuthenticationAttributeTests : IDisposable
    {
        private const string HeaderName = "x-shared-access-key",
                             HeaderNameUpper = "X-SHARED-ACCESS-KEY",
                             SecretName = "custom-access-key-name",
                             AuthorizedRoute = "/authz/shared-access-key",
                             AuthorizedRouteHeader = "/authz/shared-access-key-header",
                             AuthorizedRouteQueryString = "/authz/shared-access-key-querystring",
                             ParameterName = "api-key",
                             ParameterNameUpper = "API-KEY";

        private readonly TestApiServer _testServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationAttributeTests"/> class.
        /// </summary>
        public SharedAccessKeyAuthenticationAttributeTests(ITestOutputHelper outputWriter)
        {
            _testServer = new TestApiServer(outputWriter);
        }

        [Theory]
        [InlineData(null, null, "not empty or whitespace")]
        [InlineData("", "", "not empty or whitespace")]
        [InlineData(" ", " ", "not empty or whitespace")]
        [InlineData("not empty or whitespace", "not empty or whitespace", null)]
        [InlineData("not empty or whitespace", "not empty or whitespace", "")]
        [InlineData("not empty or whitespace", "not empty or whitespace", " ")]
        public void SharedAccessKeyAttribute_WithNotPresentHeaderNameQueryParameterNameAndOrSecretName_ShouldFailWithArgumentException(
            string headerName,
            string queryParameterName,
            string secretName)
        {
            Assert.Throws<ArgumentException>(
                () => new SharedAccessKeyAuthenticationAttribute(headerName: headerName, queryParameterName: queryParameterName, secretName: secretName));
        }

        [Theory]
        [InlineData(null, null, "not empty or whitespace")]
        [InlineData("", "", "not empty or whitespace")]
        [InlineData(" ", " ", "not empty or whitespace")]
        [InlineData("not empty or whitespace", "not empty or whitespace", null)]
        [InlineData("not empty or whitespace", "not empty or whitespace", "")]
        [InlineData("not empty or whitespace", "not empty or whitespace", " ")]
        public void SharedAccessKeyFilter_WithNotPresentHeaderNameQueryParameterNameAndOrSecretName_ShouldFailWithArgumentException(
            string headerName,
            string queryParameterName,
            string secretName)
        {
            Assert.Throws<ArgumentException>(
                () => new SharedAccessKeyAuthenticationFilter(headerName: headerName, queryParameterName: queryParameterName, secretName: secretName));
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithKeyNotFoundException_WhenNoSecretProviderWasRegistered()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: secretValue))
            {
                // Assert
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
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
            _testServer.AddServicesConfig(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider((SecretName, secretValue))));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteHeader, headerName, secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(HeaderName, "some header value")]
        [InlineData("some.header.name", "some header value")]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized(string headerName, string headerValue)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteHeader, headerName, headerValue))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenAnyHeaderValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response =
                await SendAuthorizedHttpRequest(AuthorizedRouteHeader, headerName: HeaderName, headerValues: new[] { secretValue, $"second header value other then {nameof(secretValue)}" }))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(typeof(ISecretProvider), ParameterName)]
        [InlineData(typeof(ISecretProvider), ParameterNameUpper)]
        [InlineData(typeof(ICachedSecretProvider), ParameterName)]
        [InlineData(typeof(ICachedSecretProvider), ParameterNameUpper)]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnQueryParameter_RegisteredWIthSecretProvider_ShouldNotFailWithUnauthorized(Type secretProviderType, string parameterName)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddServicesConfig(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider((SecretName, secretValue))));

            // Act
            using (HttpResponseMessage response = 
                await SendAuthorizedHttpRequest(AuthorizedRouteQueryString, headerName: null, headerValue: null, parameterName: parameterName, parameterValue: secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(ParameterName, "some parameter value")]
        [InlineData("some parameter name", "some parameter value")]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnQueryParameter_ShouldFailWithUnauthorized(string parameterName, string parameterValue)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = 
                await SendAuthorizedHttpRequest(AuthorizedRouteQueryString, headerName: null, headerValue: null, parameterName: parameterName, parameterValue: parameterValue))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(typeof(ISecretProvider))]
        [InlineData(typeof(ICachedSecretProvider))]
        public async Task AuthorizedRoute_WithSharedAccessKeyOnBothHeaderAndQueryParameter_ShouldNotFailWithUnauthorized(Type secretProviderType)
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddServicesConfig(services => services.AddSingleton(secretProviderType, new InMemorySecretProvider((SecretName, secretValue))));

            // Act
            using (HttpResponseMessage response =
                await SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: secretValue, parameterName: ParameterName, parameterValue: secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
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
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = 
                await SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: headerValue, parameterName: ParameterName, parameterValue: parameterValue))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(BypassOnMethodController.SharedAccessKeyRoute)]
        [InlineData(BypassSharedAccessKeyController.BypassOverAuthenticationRoute)]
        [InlineData(AllowAnonymousSharedAccessKeyController.Route)]
        public async Task SharedAccessKeyAuthorizedRoute_WithBypassAttributeOnMethod_SkipsAuthentication(string route)
        {
            // Arrange
            _testServer.AddFilter(new SharedAccessKeyAuthenticationFilter(HeaderName, queryParameterName: null, SecretName));
            
            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(route, headerValue: null))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpRequest(string route, string headerName = null, string headerValue = null, string parameterName = null, string parameterValue = null)
        {
            return await SendAuthorizedHttpRequest(route, headerName, new[] { headerValue }, parameterName, parameterValue);
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpRequest(string route, string headerName = null, IEnumerable<string> headerValues = null, string parameterName = null, string parameterValue = null)
        {
            string requestUri = parameterName == null ? route : route + $"?{parameterName}={parameterValue}";

            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

                if (headerName != null)
                {
                    request.Headers.Add(headerName, headerValues);
                }

                return await client.SendAsync(request);
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
