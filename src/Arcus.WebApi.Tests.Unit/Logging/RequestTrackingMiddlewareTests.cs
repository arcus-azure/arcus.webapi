using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Tests.Unit.Correlation;
using Arcus.WebApi.Tests.Unit.Hosting;
using Microsoft.AspNetCore.Builder;
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
            const string headerName = "x-custom-header", headerValue = "custom header value";
            _testServer.AddConfigure(app => app.UseMiddleware<RequestTrackingMiddleware>());
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, EchoController.Route)
                {
                    Headers = { {headerName, headerValue} },
                    Content = new StringContent("\"echo me\"", Encoding.UTF8, "application/json")
                };

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string content = await response.Content.ReadAsStringAsync();
                    Assert.Equal("echo me", content);

                    IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> eventContext = GetLoggedEventContext();
                    Assert.Contains(eventContext, item => item.Key.ToStringValue() == headerName && item.Value.ToStringValue() == $"[\"{headerValue}\"]");
                    Assert.Contains(eventContext, item => item.Key.ToStringValue() == "Body" && item.Value.ToStringValue().Trim('\\', '\"') == "echo me");
                }
            }
        }

        [Fact]
        public async Task GetRequestWithoutHeaders_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseMiddleware<NoHeadersRequestTrackingMiddleware>());
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, EchoController.Route)
                {
                    Headers = { {headerName, headerValue} },
                    Content = new StringContent($"\"{body}\"", Encoding.UTF8, "application/json")
                };

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string content = await response.Content.ReadAsStringAsync();
                    Assert.Equal("echo me", content);

                    IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> eventContext = GetLoggedEventContext();
                    Assert.DoesNotContain(eventContext, item => item.Key.ToStringValue() == headerName && item.Value.ToStringValue() == $"[\"{headerValue}\"]");
                    Assert.Contains(eventContext, item => item.Key.ToStringValue() == "Body" && item.Value.ToStringValue().Trim('\\', '\"') == body);
                }
            }
        }

        [Fact]
        public async Task GetRequestWithoutBody_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseMiddleware<NoBodyRequestTrackingMiddleware>());
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, EchoController.Route)
                {
                    Headers = { {headerName, headerValue} },
                    Content = new StringContent($"\"{body}\"", Encoding.UTF8, "application/json")
                };

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string content = await response.Content.ReadAsStringAsync();
                    Assert.Equal("echo me", content);

                    IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> eventContext = GetLoggedEventContext();
                    Assert.Contains(eventContext, item => item.Key.ToStringValue() == headerName && item.Value.ToStringValue() == $"[\"{headerValue}\"]");
                    Assert.DoesNotContain(eventContext, item => item.Key.ToStringValue() == "Body" && item.Value.ToStringValue().Trim('\\', '\"') == body);
                }
            }
        }

        private IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> GetLoggedEventContext()
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = 
                _testServer.LogSink.DequeueLogEvents()
                           .SelectMany(ev => ev.Properties);

            var eventContexts = properties.Where(prop => prop.Key == ContextProperties.EventTracking.EventContext);
            (string key, LogEventPropertyValue eventContext) = Assert.Single(eventContexts);
            var dictionaryValue = Assert.IsType<DictionaryValue>(eventContext);

            return dictionaryValue.Elements;
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
