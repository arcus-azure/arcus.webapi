using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Unit.Hosting;
using Newtonsoft.Json;
using Serilog.Events;
using Xunit;
using static Arcus.WebApi.Unit.Correlation.CorrelationController;

namespace Arcus.WebApi.Unit.Correlation
{
    public class TelemetryCorrelationTests
    {
        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task SendRequest_WithSerilogCorrelationRequestLogging_ReturnsOkWithEncrichedCorrelationLogProperties()
        {
            using (HttpClient client = _testServer.CreateClient())
            // Act
            using (HttpResponseMessage response = await client.GetAsync(Route))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                
                string json = await response.Content.ReadAsStringAsync();
                var content = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "" });
                Assert.False(String.IsNullOrWhiteSpace(content.TransactionId), "Accessed 'X-Transaction-ID' cannot be blank");
                Assert.False(String.IsNullOrWhiteSpace(content.OperationId), "Accessed 'X-Operation-ID' cannot be blank");

                IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = 
                    _testServer.LogSink.LogEvents.SelectMany(ev => ev.Properties);

                Assert.Contains(properties, prop => prop.Key == "TransactionId" && prop.Value.ToString() == $"\"{content.TransactionId}\"");
                Assert.Contains(properties, prop => prop.Key == "OperationId" && prop.Value.ToString() == $"\"{content.OperationId}\"");
            }
        }
    }
}
