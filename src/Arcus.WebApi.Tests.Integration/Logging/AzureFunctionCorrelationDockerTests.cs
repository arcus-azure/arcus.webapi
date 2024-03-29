﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly string _inProcessEndpoint = $"http://localhost:{TestConfig.GetDockerAzureFunctionsInProcessHttpPort()}/api/HttpTriggerFunction",
                                _isolatedEndpoint = $"http://localhost:{TestConfig.GetDockerAzureFunctionsIsolatedHttpPort()}/api/HttpTriggerFunction";

        private static readonly TestConfig TestConfig = TestConfig.Create();
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionCorrelationDockerTests"/> class.
        /// </summary>
        public AzureFunctionCorrelationDockerTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
            HttpClient.DefaultRequestHeaders.Remove("traceparent");
        }

        [Fact(Skip = ".NET 8 not available yet for Azure Functions in-process")]
        public async Task SendRequestInProcess_WithoutCorrelationHeaders_ResponseWithCorrelationHeadersAndCorrelationAccess()
        {
            // Act
            _logger.LogInformation("GET -> '{Uri}'", _inProcessEndpoint);
            using (HttpResponseMessage response = await HttpClient.GetAsync(_inProcessEndpoint))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, _inProcessEndpoint);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string correlationId = GetResponseHeader(response, TransactionIdHeaderName);

                string json = await response.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "", OperationParentId = "" });
                Assert.False(string.IsNullOrWhiteSpace(content.TransactionId), "Accessed 'X-Transaction-ID' cannot be blank");
                Assert.False(string.IsNullOrWhiteSpace(content.OperationId), "Accessed 'X-Operation-ID' cannot be blank");
                Assert.Null(content.OperationParentId);

                Assert.Equal(correlationId, content.TransactionId);
            }
        }

        [Fact]
        public async Task SendRequestIsolated_WithoutCorrelationHeaders_ResponseWithCorrelationHeadersAndCorrelationAccess()
        {
            // Act
            _logger.LogInformation("GET -> '{Uri}'", _isolatedEndpoint);
            using (HttpResponseMessage response = await HttpClient.GetAsync(_isolatedEndpoint))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, _isolatedEndpoint);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string correlationId = GetResponseHeader(response, TransactionIdHeaderName);

                string json = await response.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "", OperationParentId = "" });
                Assert.False(string.IsNullOrWhiteSpace(content.TransactionId), "Accessed 'X-Transaction-ID' cannot be blank");
                Assert.False(string.IsNullOrWhiteSpace(content.OperationId), "Accessed 'X-Operation-ID' cannot be blank");
                Assert.Null(content.OperationParentId);

                Assert.Equal(correlationId, content.TransactionId);
            }
        }

        [Fact(Skip = ".NET 8 not available yet for Azure Functions in-process")]
        public async Task SendRequestInProcess_WithTransactionIdHeader_ResponseWithSameCorrelationHeader()
        {
            // Arrange
            string expected = BogusGenerator.Random.Hexadecimal(32, prefix: null);
            var request = new HttpRequestMessage(HttpMethod.Get, _inProcessEndpoint);
            request.Headers.Add("traceparent", $"00-{expected}-4c6893cc6c6cad10-00");

            // Act
            _logger.LogInformation("GET -> '{Uri}'", _inProcessEndpoint);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, _inProcessEndpoint);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string json = await response.Content.ReadAsStringAsync();
                var correlationInfo = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "", OperationParentId = "" });
                
                string actual = GetResponseHeader(response, TransactionIdHeaderName);
                Assert.Equal(expected, actual);
                Assert.Equal(expected, correlationInfo.TransactionId);
            }
        }

        [Fact]
        public async Task SendRequestIsolated_WithTransactionIdHeader_ResponseWithSameCorrelationHeader()
        {
            // Arrange
            string expected = BogusGenerator.Random.Hexadecimal(32, prefix: null);
            var request = new HttpRequestMessage(HttpMethod.Get, _isolatedEndpoint);
            request.Headers.Add("traceparent", $"00-{expected}-4c6893cc6c6cad10-00");
            request.Content = new StringContent("Something to write so that we require a Content-Type");
            request.Content.Headers.Remove("Content-Type");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Act
            _logger.LogInformation("GET -> '{Uri}'", _isolatedEndpoint);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, _isolatedEndpoint);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string json = await response.Content.ReadAsStringAsync();
                var correlationInfo = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "", OperationParentId = "" });

                string actual = GetResponseHeader(response, TransactionIdHeaderName);
                Assert.Equal(expected, actual);
                Assert.Equal(expected, correlationInfo.TransactionId);
            }
        }

        [Fact(Skip = ".NET 8 not available yet for Azure Functions in-process")]
        public async Task SendRequestInProcess_WithRequestIdHeader_ResponseWithSameRequestIdHeader()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, _inProcessEndpoint);
            var expected = $"00-4b1c0c8d608f57db7bd0b13c88ef865e-{BogusGenerator.Random.Hexadecimal(16, prefix: null)}-00";
            request.Headers.Add("traceparent", expected);

            // Act
            _logger.LogInformation("GET -> '{Uri}'", _inProcessEndpoint);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, _inProcessEndpoint);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string actual = GetResponseHeader(response, "traceparent");
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public async Task SendRequestIsolated_WithRequestIdHeader_ResponseWithSameRequestIdHeader()
        {
            // Arrange
            string expected = BogusGenerator.Random.Hexadecimal(16, prefix: null);
            var request = new HttpRequestMessage(HttpMethod.Get, _isolatedEndpoint);
            request.Headers.Add("traceparent", $"00-4b1c0c8d608f57db7bd0b13c88ef865e-{expected}-00");
            request.Content = new StringContent("Something to write so that we require a Content-Type");
            request.Content.Headers.Remove("Content-Type");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Act
            _logger.LogInformation("GET -> '{Uri}'", _isolatedEndpoint);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, _isolatedEndpoint);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string json = await response.Content.ReadAsStringAsync();
                var correlationInfo = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "", OperationParentId = "" });

                string actual = GetResponseHeader(response, "traceparent");
                Assert.Contains(expected, actual);
                Assert.Equal(expected, correlationInfo.OperationParentId);
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
