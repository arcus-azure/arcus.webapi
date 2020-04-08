using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.WebApi.Tests.Unit.Hosting;
using Serilog.Events;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class RequestTrackingMiddlewareTests : IDisposable
    {
        private readonly TestApiServer _testServer = new TestApiServer();

        [Fact]
        public async Task GetRequest_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, EchoController.Route)
                {
                    Content = new StringContent("echo me!", Encoding.UTF8, "text/plain")
                };

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    IEnumerable<LogEvent> logEvents = _testServer.LogSink.DequeueLogEvents();
                    
                }
            }
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
