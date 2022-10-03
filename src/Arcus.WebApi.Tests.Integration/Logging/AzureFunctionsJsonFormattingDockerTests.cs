using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging;
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
            // Act
            using (HttpResponseMessage response = await HttpClient.GetAsync(Endpoint))
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
            request.Headers.TryAddWithoutValidation("allow", "application/json");

            // Act
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async Task Request_WithoutAllowHeader_ReturnsFailure()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
            request.Headers.TryAddWithoutValidation("content-Type", "application/json");

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
            request.Headers.TryAddWithoutValidation("content-type", "application/json");
            request.Headers.TryAddWithoutValidation("allow", "application/json");

            // Act
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
