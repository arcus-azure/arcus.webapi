using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Interfaces;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Arcus.WebApi.Unit.Hosting;
using Xunit;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    public class SharedAccessKeyAuthenticationAttributeTests : IDisposable
    {
        private const string HeaderName = "x-shared-access-key",
                             SecretName = "custom-access-key-name",
                             AuthorizedRoute = "/authz/shared-access-key",
                             AuthorizedRouteHeader = "/authz/shared-access-key-header",
                             AuthorizedRouteQueryString = "/authz/shared-access-key-querystring",
                             ParameterName = "api-key";

        private readonly TestApiServer _testServer = new TestApiServer();

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
                () => new SharedAccessKeyAuthenticationAttribute(headerName, queryParameterName, secretName));
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
                () => new SharedAccessKeyAuthenticationFilter(headerName, queryParameterName, secretName));
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithKeyNotFoundException_WhenNoSecretProviderWasRegistered()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: secretValue));
        }

        #region HeaderValidation

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithSecretProvider_ShouldNotFailWithUnauthorized_WhenOnlyHeaderProvidedAndHeaderValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteHeader, headerName: HeaderName, headerValue: secretValue, parameterName: null, parameterValue: null))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithCachedSecretProvider_ShouldNotFailWithUnauthorized_WhenOnlyHeaderProvidedAndHeaderValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ICachedSecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteHeader, headerName: HeaderName, headerValue: secretValue, parameterName: null, parameterValue: null))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenHeaderValueNotMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteHeader, headerName: HeaderName, headerValue: $"something else then {nameof(secretValue)}"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenHeaderIsNotPresent()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteHeader, headerName: $"something-else-then-{nameof(HeaderName)}", headerValue: "some header value"))
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

        #endregion HeaderValidation

        #region QueryStringValidation

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithSecretProvider_ShouldNotFailWithUnauthorized_WhenOnlyQueryStringProvidedAndQueryStringValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteQueryString, headerName: null, headerValue: null, parameterName: ParameterName, parameterValue: secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithCachedSecretProvider_ShouldNotFailWithUnauthorized_WhenOnlyQueryStringProvidedAndQueryStringValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ICachedSecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteQueryString, headerName: null, headerValue: null, parameterName: ParameterName, parameterValue: secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenQueryStringValueNotMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteQueryString, headerName: null, headerValue: null, parameterName: ParameterName, parameterValue: $"something else then {nameof(secretValue)}"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenQueryStringParameterIsNotPresent()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRouteQueryString, headerName: null, headerValue: null, parameterName: $"something-else-then-{nameof(ParameterName)}", parameterValue: "some parameter value"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        #endregion QueryStringValidation

        #region HeaderAndQueryStringValidation

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithSecretProvider_ShouldNotFailWithUnauthorized_WhenBothHeaderAndQueryStringValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: secretValue, parameterName: ParameterName, parameterValue: secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithCachedSecretProvider_ShouldNotFailWithUnauthorized_WhenBothHeaderAndQueryStringValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ICachedSecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: secretValue, parameterName: ParameterName, parameterValue: secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenBothHeaderValueAndQueryStringValueNotMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: $"something else then {nameof(secretValue)}", parameterName: ParameterName, parameterValue: $"something else then {nameof(secretValue)}"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenHeaderValueNotMatchesSecretValueAndQueryStringValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: $"something else then {nameof(secretValue)}", parameterName: ParameterName, parameterValue: secretValue))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenQueryStringValueNotMatchesSecretValueAndHeaderValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequest(AuthorizedRoute, headerName: HeaderName, headerValue: secretValue, parameterName: ParameterName, parameterValue: $"something else then {nameof(secretValue)}"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        #endregion HeaderAndQueryStringValidation

        private async Task<HttpResponseMessage> SendAuthorizedHttpRequest(string route, string headerName = null, string headerValue = null, string parameterName = null, string parameterValue = null)
        {
            return await SendAuthorizedHttpRequest(route, headerName, new[] { headerValue }, parameterName, parameterValue);
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpRequest(string route, string headerName = null, IEnumerable<string> headerValues = null, string parameterName = null, string parameterValue = null)
        {
            string requestUri = parameterName == null ? route : route + $"?{parameterName}={parameterValue}";
            HttpRequestMessage request;

            if (headerName == null)
            {
                request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Get, requestUri)
                {
                    Headers = { { headerName, headerValues } }
                };
            }

            return await SendAuthorizedHttpRequest(request);
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpRequest(HttpRequestMessage httpRequestMessage)
        {
            using (HttpClient client = _testServer.CreateClient())
            {
                return await client.SendAsync(httpRequestMessage);
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
