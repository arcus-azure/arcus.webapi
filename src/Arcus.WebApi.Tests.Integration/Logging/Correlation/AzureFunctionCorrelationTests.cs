﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.Logging.Correlation
{
    public class AzureFunctionCorrelationTests
    {
        private const string DefaultOperationId = "RequestId",
                             DefaultTransactionId = "X-Transaction-ID";

        private readonly TestConfig _config;
        private readonly XunitTestLogger _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionCorrelationTests"/> class.
        /// </summary>
        public AzureFunctionCorrelationTests(ITestOutputHelper outputWriter)
        {
            _config = TestConfig.Create();
            _logger = new XunitTestLogger(outputWriter);
            
            int httpPort = _config.GetHttpPort();
            DefaultRoute = $"http://localhost:{httpPort}/api/HttpTriggerFunction";
        }

        public string DefaultRoute { get; }

        [Fact]
        public async Task SendRequest_WithoutCorrelationHeaders_ResponseWithCorrelationHeadersAndCorrelationAccess()
        {
            // Act
            _logger.LogInformation("GET -> '{Uri}'", DefaultRoute);
            using (HttpResponseMessage response = await HttpClient.GetAsync(DefaultRoute))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, DefaultRoute);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string correlationId = GetResponseHeader(response, DefaultTransactionId);
                string requestId = GetResponseHeader(response, DefaultOperationId);

                string json = await response.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "" });
                Assert.False(String.IsNullOrWhiteSpace(content.TransactionId), "Accessed 'X-Transaction-ID' cannot be blank");
                Assert.False(String.IsNullOrWhiteSpace(content.OperationId), "Accessed 'X-Operation-ID' cannot be blank");

                Assert.Equal(correlationId, content.TransactionId);
                Assert.Equal(requestId, content.OperationId);
            }
        }

        [Fact]
        public async Task SendRequest_WithTransactionIdHeader_ResponseWithSameCorrelationHeader()
        {
            // Arrange
            string expected = $"transaction-{Guid.NewGuid()}";
            var request = new HttpRequestMessage(HttpMethod.Get, DefaultRoute);
            request.Headers.Add(DefaultTransactionId, expected);

            // Act
            _logger.LogInformation("GET -> '{Uri}'", DefaultRoute);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, DefaultRoute);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string actual = GetResponseHeader(response, DefaultTransactionId);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public async Task SendRequest_WithRequestIdHeader_ResponseWithDifferentRequestIdHeader()
        {
            // Arrange
            string expected = $"operation-{Guid.NewGuid()}";
            var request = new HttpRequestMessage(HttpMethod.Get, DefaultRoute);
            request.Headers.Add(DefaultOperationId, expected);

            // Act
            _logger.LogInformation("GET -> '{Uri}'", DefaultRoute);
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                // Assert
                _logger.LogInformation("{StatusCode} <- {Uri}", response.StatusCode, DefaultRoute);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string actual = GetResponseHeader(response, DefaultOperationId);
                Assert.NotEqual(expected, actual);
            }
        }

        private static string GetResponseHeader(HttpResponseMessage response, string headerName)
        {
            (string key, IEnumerable<string> values) = Assert.Single(response.Headers, header => header.Key == headerName);

            Assert.NotNull(values);
            string value = Assert.Single(values);
            Assert.False(String.IsNullOrWhiteSpace(value), $"Response header '{headerName}' cannot be blank");

            return value;
        }
    }
}
