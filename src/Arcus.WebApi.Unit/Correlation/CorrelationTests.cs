using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Correlation;
using Arcus.WebApi.Unit.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using static Arcus.WebApi.Unit.Correlation.CorrelationController;

namespace Arcus.WebApi.Unit.Correlation
{
    public class CorrelationTests
    {
        private const string DefaultOperationId = "X-Operation-ID",
                             DefaultTransactionId = "X-Transaction-ID";

        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNotAllowTransactionInRequest_ResponseWithBadRequest()
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.Configure<CorrelationOptions>(options => options.Transaction.AllowInRequest = false));
            
            using (HttpClient client = _testServer.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, Route))
            {
                request.Headers.Add(DefaultTransactionId, Guid.NewGuid().ToString());

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultOperationId);
                    Assert.DoesNotContain(response.Headers, header => header.Key == DefaultTransactionId);
                }
            }
        }

        [Fact]
        public async Task SendRequest_WithCorrelationOptionsNotGenerateTransactionId_ResponseWithoutTransactionId()
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.Configure<CorrelationOptions>(options => options.Transaction.GenerateWhenNotSpecified = false));

            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(Route))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(response.Headers, header => header.Key == DefaultOperationId);
                (string transactionIdHeaderName, IEnumerable<string> transactionIds) = 
                    Assert.Single(response.Headers, header => header.Key == DefaultTransactionId);
                Assert.Empty(transactionIds);
            }
        }

        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNonTransactionIncludeInResponse_ResponseWithoutCorrelationHeaders()
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.Configure<CorrelationOptions>(options => options.Transaction.IncludeInResponse = false));
            
            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(Route))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(response.Headers, header => header.Key == DefaultOperationId);
                Assert.DoesNotContain(response.Headers, header => header.Key == DefaultTransactionId);
            }
        }

        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNonOperationIncludeInResponse_ResponseWithoutCorrelationHeaders()
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.Configure<CorrelationOptions>(options => options.Operation.IncludeInResponse = false));
            
            using (HttpClient client = _testServer.CreateClient())
                // Act
            using (HttpResponseMessage response = await client.GetAsync(Route))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.DoesNotContain(response.Headers, header => header.Key == DefaultOperationId);
                Assert.Contains(response.Headers, header => header.Key == DefaultTransactionId);
            }
        }

        [Fact]
        public async Task SendRequest_WithoutCorrelationHeaders_ResponseWithCorrelationHeadersAndCorrelationAccess()
        {
            // Arrange
            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(Route))
            {
                // Assert
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
        public async Task SendRequest_WithCorrelationHeader_ResponseWithSameCorrelationHeader()
        {
            // Arrange
            string expected = $"correlationId-{Guid.NewGuid()}";
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, Route);
                request.Headers.Add(DefaultTransactionId, expected);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string actual = GetResponseHeader(response, DefaultTransactionId);
                    Assert.Equal(expected, actual);
                }
            }
        }

        [Fact]
        public async Task SendRequest_WithRequestIdHeader_ResponseWithDifferentRequestIdHeader()
        {
            // Arrange
            string expected = $"requestId-{Guid.NewGuid()}";
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, Route);
                request.Headers.Add(DefaultOperationId, expected);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string actual = GetResponseHeader(response, DefaultOperationId);
                    Assert.NotEqual(expected, actual);
                }
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
