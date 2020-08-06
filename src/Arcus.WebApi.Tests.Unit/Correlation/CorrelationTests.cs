using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Tests.Unit.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using static Arcus.WebApi.Tests.Unit.Correlation.CorrelationController;

namespace Arcus.WebApi.Tests.Unit.Correlation
{
    public class CorrelationTests : IDisposable
    {
        private const string DefaultOperationId = "RequestId",
                             DefaultTransactionId = "X-Transaction-ID";

        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNotAllowTransactionInRequest_ResponseWithBadRequest()
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.Configure<CorrelationInfoOptions>(options => options.Transaction.AllowInRequest = false));
            
            using (HttpClient client = _testServer.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, DefaultRoute))
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
        public async Task SendRequest_WithCorrelationInfoOptionsNotGenerateTransactionId_ResponseWithoutTransactionId()
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.Configure<CorrelationInfoOptions>(options => options.Transaction.GenerateWhenNotSpecified = false));

            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(DefaultRoute))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(response.Headers, header => header.Key == DefaultOperationId);
                Assert.DoesNotContain(response.Headers, header => header.Key == DefaultTransactionId);
            }
        }

        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNonTransactionIncludeInResponse_ResponseWithoutCorrelationHeaders()
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.Configure<CorrelationInfoOptions>(options => options.Transaction.IncludeInResponse = false));
            
            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(DefaultRoute))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(response.Headers, header => header.Key == DefaultOperationId);
                Assert.DoesNotContain(response.Headers, header => header.Key == DefaultTransactionId);
            }
        }

        [Fact]
        public async Task SendRequest_WithCorrelateOptionsCustomGenerateTransactionId_ResponseWitCustomGeneratedTransactionId()
        {
            // Arrange
            var expectedTransactionId = $"transaction-{Guid.NewGuid():N}";
            _testServer.AddServicesConfig(services => services.Configure<CorrelationInfoOptions>(options => options.Transaction.GenerateId = () => expectedTransactionId));

            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(DefaultRoute))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(response.Headers, header => header.Key == DefaultOperationId);
                
                string actualTransactionId = GetResponseHeader(response, DefaultTransactionId);
                Assert.Equal(expectedTransactionId, actualTransactionId);
            }
        }

        [Fact]
        public async Task SendRequest_WithCorrelateOptionsNonOperationIncludeInResponse_ResponseWithoutCorrelationHeaders()
        {
            // Arrange
            _testServer.AddServicesConfig(services => services.Configure<CorrelationInfoOptions>(options => options.Operation.IncludeInResponse = false));
            
            using (HttpClient client = _testServer.CreateClient()) 
            // Act
            using (HttpResponseMessage response = await client.GetAsync(DefaultRoute))
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
            using (HttpResponseMessage response = await client.GetAsync(DefaultRoute))
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
            string expected = $"transaction-{Guid.NewGuid()}";
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, DefaultRoute);
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
            string expected = $"operation-{Guid.NewGuid()}";
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, DefaultRoute);
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

        [Fact]
        public async Task SendRequest_WithCorrelateOptionsCustomGenerateOperationId_ResponseWitCustomGeneratedOperationId()
        {
            // Arrange
            var expectedOperationId = $"operation-{Guid.NewGuid():N}";
            _testServer.AddServicesConfig(services => services.Configure<TraceIdentifierOptions>(options => options.EnableTraceIdentifier = false));
            _testServer.AddServicesConfig(services => services.Configure<CorrelationInfoOptions>(options => options.Operation.GenerateId = () => expectedOperationId));

            using (HttpClient client = _testServer.CreateClient())
                // Act
            using (HttpResponseMessage response = await client.GetAsync(DefaultRoute))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(response.Headers, header => header.Key == DefaultTransactionId);
                
                string actualOperationId = GetResponseHeader(response, DefaultOperationId);
                Assert.Equal(expectedOperationId, actualOperationId);
            }
        }

        [Fact]
        public async Task SendRequestWithCorrelateInfo_SetsCorrelationInfo_ResponseWithUpdatedCorrelationInfo()
        {
            // Arrange
            var correlationInfo = new CorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}");

            using (HttpClient client = _testServer.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, SetCorrelationRoute))
            {
                request.Headers.Add(DefaultOperationId, correlationInfo.OperationId);
                request.Headers.Add(DefaultTransactionId, correlationInfo.TransactionId);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string actualOperationId = GetResponseHeader(response, DefaultOperationId);
                    string actualTransactionId = GetResponseHeader(response, DefaultTransactionId);

                    Assert.Equal(correlationInfo.OperationId, actualOperationId);
                    Assert.Equal(correlationInfo.TransactionId, actualTransactionId);
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _testServer?.Dispose();
        }
    }
}
