using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Tests.Unit.Hosting;
using Newtonsoft.Json;
using Serilog.Events;
using Xunit;
using static Arcus.WebApi.Tests.Unit.Correlation.CorrelationController;

namespace Arcus.WebApi.Tests.Unit.Correlation
{
    public class TelemetryCorrelationTests : IDisposable
    {
        private const string TransactionIdPropertyName = "TransactionId",
                             OperationIdPropertyName = "OperationId";

        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task SendRequest_WithSerilogCorrelationEnrichment_ReturnsOkWithEnrichedCorrelationLogProperties()
        {
            // Arrange
            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(Route))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                CorrelationInfo correlationInfo = await AssertAppCorrelationInfoAsync(response);
                AssertLoggedCorrelationProperties(correlationInfo);
            }
        }

        [Fact]
        public async Task SendRequest_WithSerilogCorrelationenrichment_ReturnsOkWithDifferentOperationIdAndSameTransactionId()
        {
            // Arrange
            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage firstResponse = await client.GetAsync(Route))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
                CorrelationInfo firstCorrelationInfo = await AssertAppCorrelationInfoAsync(firstResponse);
                AssertLoggedCorrelationProperties(firstCorrelationInfo);

                var request = new HttpRequestMessage(HttpMethod.Get, Route);
                request.Headers.Add("X-Transaction-ID", firstCorrelationInfo.TransactionId);
                
                using (HttpResponseMessage secondResponse = await client.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
                    CorrelationInfo secondCorrelationInfo = await AssertAppCorrelationInfoAsync(secondResponse);
                    AssertLoggedCorrelationProperties(secondCorrelationInfo);

                    Assert.NotEqual(firstCorrelationInfo.OperationId, secondCorrelationInfo.OperationId);
                    Assert.Equal(firstCorrelationInfo.TransactionId, secondCorrelationInfo.TransactionId);
                }
            }
        }

        [Fact]
        public async Task SendRequest_WithSerilogCorrelationenrichment_ReturnsOkWithDifferentOperationIdAndDifferentTransactionId()
        {
            // Arrange
            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage firstResponse = await client.GetAsync(Route))
            {
                // Assert
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
                CorrelationInfo firstCorrelationInfo = await AssertAppCorrelationInfoAsync(firstResponse);
                AssertLoggedCorrelationProperties(firstCorrelationInfo);

                using (HttpResponseMessage secondResponse = await client.GetAsync(Route))
                {
                    Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
                    CorrelationInfo secondCorrelationInfo = await AssertAppCorrelationInfoAsync(secondResponse);
                    AssertLoggedCorrelationProperties(secondCorrelationInfo);
                    
                    Assert.NotEqual(firstCorrelationInfo.OperationId, secondCorrelationInfo.OperationId);
                    Assert.NotEqual(firstCorrelationInfo.TransactionId, secondCorrelationInfo.TransactionId);
                }
            }
        }

        private static async Task<CorrelationInfo> AssertAppCorrelationInfoAsync(HttpResponseMessage response)
        {
            string json = await response.Content.ReadAsStringAsync();
            Assert.False(String.IsNullOrWhiteSpace(json), "No HTTP response content available");
            var content = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "" });
            Assert.False(String.IsNullOrWhiteSpace(content.TransactionId), "Accessed 'X-Transaction-ID' cannot be blank");
            Assert.False(String.IsNullOrWhiteSpace(content.OperationId), "Accessed 'X-Operation-ID' cannot be blank");

            return new CorrelationInfo(content.OperationId, content.TransactionId);
        }

        private void AssertLoggedCorrelationProperties(CorrelationInfo correlationInfo)
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = 
                _testServer.LogSink.DequeueLogEvents()
                           .SelectMany(ev => ev.Properties);

            var transactionIdProperties = properties.Where(prop => prop.Key == TransactionIdPropertyName);
            var operationIdProperties = properties.Where(prop => prop.Key == OperationIdPropertyName);

            Assert.Contains(transactionIdProperties, prop => correlationInfo.TransactionId == prop.Value.ToStringValue());
            Assert.Contains(operationIdProperties, prop => correlationInfo.OperationId == prop.Value.ToStringValue());
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
