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
using static Correlate.Http.CorrelationHttpHeaders;

namespace Arcus.WebApi.Unit.Correlation
{
    public class CorrelationTests
    {
        private readonly TestApiServer _testServer = new TestApiServer();

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
                Assert.DoesNotContain(response.Headers, header => header.Key == CorrelationId);
                Assert.Contains(response.Headers, header => header.Key == RequestId);
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
                Assert.Contains(response.Headers, header => header.Key == RequestId);
                Assert.DoesNotContain(response.Headers, header => header.Key == CorrelationId);
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
                
                string correlationId = GetResponseHeader(response, CorrelationId);
                string requestId = GetResponseHeader(response, RequestId);

                string json = await response.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeAnonymousType(json, new { CorrelationId = "", RequestId = "" });
                Assert.False(String.IsNullOrWhiteSpace(content.CorrelationId), "Accessed 'CorrelationId' cannot be blank");
                Assert.False(String.IsNullOrWhiteSpace(content.RequestId), "Accessed 'RequestId' cannot be blank");
                
                Assert.Equal(correlationId, content.CorrelationId);
                Assert.Equal(requestId, content.RequestId);
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
                request.Headers.Add(CorrelationId, expected);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string actual = GetResponseHeader(response, CorrelationId);
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
                request.Headers.Add(RequestId, expected);

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string actual = GetResponseHeader(response, RequestId);
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
