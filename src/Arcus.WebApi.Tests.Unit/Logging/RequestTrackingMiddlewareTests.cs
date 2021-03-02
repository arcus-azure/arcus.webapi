using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
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
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking());
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, EchoController.Route)
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
                    Assert.True(ContainsRequestHeader(eventContext, headerName, headerValue), "Logged event context should contain request header");
                    Assert.False(ContainsRequestBody(eventContext, body), "Shouldn't contain request body");
                }
            }
        }

        [Fact]
        public async Task GetRequest_TracksRequestWithoutHeaders_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking<NoHeadersRequestTrackingMiddleware>());
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, EchoController.Route)
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
                    Assert.False(ContainsRequestHeader(eventContext, headerName, headerValue), "Logged event context shouldn't contain request header");
                    Assert.False(ContainsRequestBody(eventContext, body), "Logged event context shouldn't contain request body");
                }
            }
        }

        [Theory]
        [InlineData("Authentication")]
        [InlineData("X-Api-Key")]
        [InlineData("X-ARR-ClientCert")]
        public async Task GetRequestWithDefaultOmittedHeader_TracksRequestWithoutHeader_ReturnsSuccess(string omittedHeaderName)
        {
            // Arrange
            const string customHeaderName = "x-custom-header", customHeaderValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking());
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, EchoController.Route)
                {
                    Headers =
                    {
                        {omittedHeaderName, Guid.NewGuid().ToString()}, 
                        {customHeaderName, customHeaderValue}
                    },
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
                    Assert.True(ContainsRequestHeader(eventContext, customHeaderName, customHeaderValue), "Logged event context should contain request header");
                    Assert.DoesNotContain(eventContext, item => item.Key.ToStringValue() == omittedHeaderName);
                    Assert.False(ContainsRequestBody(eventContext, body), "Logged event context shouldn't contain request body");
                }
            }
        }

        [Fact]
        public async Task GetRequest_TracksWithCustomOmittedHeader_ReturnsSuccess()
        {
            // Arrange
            const string customHeaderName = "x-custom-secret-header", customHeaderValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking(options => options.OmittedHeaderNames.Add(customHeaderName)));
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, EchoController.Route)
                {
                    Headers = { {customHeaderName, customHeaderValue} },
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
                    Assert.False(ContainsRequestHeader(eventContext, customHeaderName, customHeaderValue), "Logged event context shouldn't contain request header");
                    Assert.False(ContainsRequestBody(eventContext, body), "Logged event context shouldn't contain request body");
                }
            }
        }

        [Fact]
        public async Task GetRequestWithoutHeaders_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking(options => options.IncludeRequestHeaders = false));
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, EchoController.Route)
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
                    Assert.False(ContainsRequestHeader(eventContext, headerName, headerValue), "Logged event context shouldn't contain request header");
                    Assert.False(ContainsRequestBody(eventContext, body), "Logged event context shouldn't contain request body");
                }
            }
        }

        [Fact]
        public async Task GetRequestWithBody_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking(options => options.IncludeRequestBody = true));
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, EchoController.Route)
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
                    Assert.True(ContainsRequestHeader(eventContext, headerName, headerValue), "Logged event context should contain request header");
                    Assert.True(ContainsRequestBody(eventContext, body), "Logged event context should contain request body");
                }
            }
        }

        [Fact]
        public async Task GetRequestWithSkippedAttributeOnMethod_SkipsRequestTracking_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "some body";
            _testServer.AddConfigure(app => app.UseRequestTracking());
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, SkipRequestTrackingOnMethodController.Route)
                {
                    Headers = { {headerName, headerValue} },
                    Content = new StringContent($"\"{body}\"", Encoding.UTF8, "application/json")
                };

                // Act
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.False(ContainsLoggedEventContext(), "No event context should be logged when the skipped request tracking attribute is applied");
                }
            }
        }

        private bool ContainsLoggedEventContext()
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = 
                _testServer.LogSink.DequeueLogEvents()
                           .SelectMany(ev => ev.Properties);

            var eventContexts = properties.Where(prop => prop.Key == ContextProperties.TelemetryContext);
            return eventContexts.Any();
        }

        private IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> GetLoggedEventContext()
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = 
                _testServer.LogSink.DequeueLogEvents()
                           .SelectMany(ev => ev.Properties);

            var eventContexts = properties.Where(prop => prop.Key == ContextProperties.TelemetryContext);
            (string key, LogEventPropertyValue eventContext) = Assert.Single(eventContexts);
            var dictionaryValue = Assert.IsType<DictionaryValue>(eventContext);

            return dictionaryValue.Elements;
        }

        private static bool ContainsRequestHeader(IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> eventContext, string headerName, string headerValue)
        {
            return eventContext.Any(item => item.Key.ToStringValue() == headerName && item.Value.ToStringValue() == $"[\"{headerValue}\"]");
        }

        private static bool ContainsRequestBody(IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> eventContext, string expectedBody)
        {
            return eventContext.Any(item => item.Key.ToStringValue() == "Body" && item.Value.ToStringValue().Trim('\\', '\"', '[', ']') == expectedBody);
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
