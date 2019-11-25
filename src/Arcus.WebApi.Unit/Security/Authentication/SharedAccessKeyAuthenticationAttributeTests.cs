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
                             ParameterName = "api-key";

        private readonly TestApiServer _testServer = new TestApiServer();

        [Theory]
        [InlineData(null, null, "not empty or whitespace")]
        [InlineData("", "", "not empty or whitespace")]
        [InlineData(" ", " ", "not empty or whitespace")]
        [InlineData("not empty or whitespace", "not empty or whitespace", null)]
        [InlineData("not empty or whitespace", "not empty or whitespace", "")]
        [InlineData("not empty or whitespace", "not empty or whitespace", " ")]
        public void SharedAccessKeyAttribute_WithNotPresentHeaderNameAndOrSecretName_ShouldFailWithArgumentException(
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
        public void SharedAccessKeyFilter_WithNotPresentHeaderNameAndOrSecretName_ShouldFailWithArgumentException(
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
                () => SendAuthorizedHttpRequestWithHeader(HeaderName, secretValue));
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey__RegisteredWithSecretProvider_ShouldNotFailWithUnauthorized_WhenHeaderValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequestWithHeader(HeaderName, secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithCachedSecretProvider_ShouldNotFailWithUnauthorized_WhenHeaderValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ICachedSecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequestWithHeader(HeaderName, secretValue))
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
            using (HttpResponseMessage response = await SendAuthorizedHttpRequestWithHeader(HeaderName, $"something else then {nameof(secretValue)}"))
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
            using (HttpResponseMessage response = await SendAuthorizedHttpRequestWithHeader($"something-else-then-{nameof(HeaderName)}", "some header value"))
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
                await SendAuthorizedHttpRequestWithHeader(HeaderName, new[] { secretValue, $"second header value other then {nameof(secretValue)}" }))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey__RegisteredWithSecretProvider_ShouldNotFailWithUnauthorized_WhenQuerystringValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequestWithQuerystring(ParameterName, secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_RegisteredWithCachedSecretProvider_ShouldNotFailWithUnauthorized_WhenQuerystringValueMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ICachedSecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequestWithQuerystring(ParameterName, secretValue))
            {
                // Assert
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenQuerystringValueNotMatchesSecretValue()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequestWithQuerystring(ParameterName, $"something else then {nameof(secretValue)}"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task AuthorizedRoute_WithSharedAccessKey_ShouldFailWithUnauthorized_WhenQuerystringParameterIsNotPresent()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            _testServer.AddService<ISecretProvider>(new InMemorySecretProvider((SecretName, secretValue)));

            // Act
            using (HttpResponseMessage response = await SendAuthorizedHttpRequestWithQuerystring($"something-else-then-{nameof(ParameterName)}", "some parameter value"))
            {
                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }
        
        private Task<HttpResponseMessage> SendAuthorizedHttpRequestWithHeader(string headerName, string headerValue)
        {
            return SendAuthorizedHttpRequestWithHeader(headerName, new[] { headerValue });
        }

        private async Task<HttpResponseMessage> SendAuthorizedHttpRequestWithHeader(string headerName, IEnumerable<string> headerValues)
        {
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, AuthorizedRoute)
                {
                    Headers = { { headerName, headerValues } }
                };

                return await client.SendAsync(request);
            }
        }
        private async Task<HttpResponseMessage> SendAuthorizedHttpRequestWithQuerystring(string parameterName, string parameterValue)
        {
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, AuthorizedRoute + $"?{parameterName}={parameterValue}");

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
