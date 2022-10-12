using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Bogus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using static Arcus.WebApi.Logging.Core.Correlation.HttpCorrelationProperties;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection(Constants.TestCollections.Docker)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Docker)]
    public class AzureFunctionCorrelationDockerTests
    {
        private readonly XunitTestLogger _logger;

        private static readonly TestConfig TestConfig = TestConfig.Create();
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionCorrelationDockerTests"/> class.
        /// </summary>
        public AzureFunctionCorrelationDockerTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        public static IEnumerable<object[]> RunningAzureFunctionsDockerProjectUrls => new[]
        {
            new object[] { $"http://localhost:{TestConfig.GetDockerAzureFunctionsInProcessHttpPort()}/api/HttpTriggerFunction" },
            new object[] { $"http://localhost:{TestConfig.GetDockerAzureFunctionsIsolatedHttpPort()}/api/HttpTriggerFunction" }
        };

        [Theory]
        [MemberData(nameof(RunningAzureFunctionsDockerProjectUrls))]
        public async Task SendRequest_WithoutCorrelationHeaders_ResponseWithCorrelationHeadersAndCorrelationAccess(string url)
        {
            // Act
            _logger.LogInformation("GET -> '{Uri}'", url);
            using (HttpResponseMessage response = await HttpClient.GetAsync(url))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, url);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string correlationId = GetResponseHeader(response, TransactionIdHeaderName);

                string json = await response.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "", OperationParentId = "" });
                Assert.False(string.IsNullOrWhiteSpace(content.TransactionId), "Accessed 'X-Transaction-ID' cannot be blank");
                Assert.False(string.IsNullOrWhiteSpace(content.OperationId), "Accessed 'X-Operation-ID' cannot be blank");
                Assert.NotNull(content.OperationParentId);

                Assert.Equal(correlationId, content.TransactionId);
            }
        }

        [Theory]
        [MemberData(nameof(RunningAzureFunctionsDockerProjectUrls))]
        public async Task SendRequest_WithTransactionIdHeader_ResponseWithSameCorrelationHeader(string url)
        {
            // Arrange
            string expected = BogusGenerator.Random.Hexadecimal(32, prefix: null);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("traceparent", $"00-{expected}-4c6893cc6c6cad10-00");

            // Act
            _logger.LogInformation("GET -> '{Uri}'", url);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, url);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string actual = GetResponseHeader(response, TransactionIdHeaderName);
                Assert.Equal(expected, actual);
            }
        }

        [Theory]
        [MemberData(nameof(RunningAzureFunctionsDockerProjectUrls))]
        public async Task SendRequest_WithRequestIdHeader_ResponseWithDifferentRequestIdHeader(string url)
        {
            // Arrange
            string expected = BogusGenerator.Random.Hexadecimal(16, prefix: null);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add(UpstreamServiceHeaderName, $"00-4b1c0c8d608f57db7bd0b13c88ef865e-{expected}-00");

            // Act
            _logger.LogInformation("GET -> '{Uri}'", url);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, url);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string actual = GetResponseHeader(response, UpstreamServiceHeaderName);
                Assert.Equal(expected, actual);
            }
        }

        private static string GetResponseHeader(HttpResponseMessage response, string headerName)
        {
            (string key, IEnumerable<string> values) = Assert.Single(response.Headers, header => header.Key == headerName);

            Assert.NotNull(values);
            string value = Assert.Single(values);
            Assert.False(string.IsNullOrWhiteSpace(value), $"Response header '{headerName}' cannot be blank");

            return value;
        }
    }
}
