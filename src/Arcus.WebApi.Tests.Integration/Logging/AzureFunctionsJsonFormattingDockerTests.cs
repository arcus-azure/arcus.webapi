using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection(Constants.TestCollections.Docker)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Docker)]
    public class AzureFunctionsJsonFormattingDockerTests
    {
        private readonly TestConfig _config;
        private readonly ILogger _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsJsonFormattingDockerTests" /> class.
        /// </summary>
        public AzureFunctionsJsonFormattingDockerTests(ITestOutputHelper outputWriter)
        {
            _config = TestConfig.Create();
            _logger = new XunitTestLogger(outputWriter);
        }

        private string Endpoint => $"http://localhost:{_config.GetDockerAzureFunctionsIsolatedHttpPort()}/api/HttpTriggerFunction";

        [Fact]
        public async Task Request_WithoutJsonFormattingHeaders_ReturnsFailure()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
            request.Content = new StringContent("Something to write so that we required a Content-Type");

            // Act
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task Request_WithWrongContentType_ReturnsFailure()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
            request.Content = new StringContent("Something to write so that we required a Content-Type", Encoding.UTF8, "text/plain");

            // Act
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task Request_WithoutContentTypeHeader_ReturnsFailure()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
            request.Content = new StringContent("Something to write so that we required a Content-Type");
            request.Headers.TryAddWithoutValidation("allow", "application/json");

            // Act
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task Request_WithWrongAllowHeader_ReturnsFailure()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
            request.Content = new StringContent("Something to write so that we require a Content-Type");
            request.Headers.TryAddWithoutValidation("content-type", "application/json");
            request.Headers.TryAddWithoutValidation("allow", "text/plain");

            // Act
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task Request_WithJsonAllowAndContentTypeHeaders_ReturnsFailure()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
            request.Content = new StringContent("Something to write so that we require a Content-Type");
            request.Content.Headers.Remove("Content-Type");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Act
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
