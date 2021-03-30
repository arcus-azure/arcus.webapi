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
using Bogus;
using Microsoft.AspNetCore.Builder;
using Serilog.Events;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class RequestTrackingMiddlewareTests : IDisposable
    {
        private const string RequestBodyKey = "RequestBody",
                             ResponseBodyKey = "ResponseBody";
        
        private readonly TestApiServer _testServer = new TestApiServer();
        private readonly Faker _bogusGenerator = new Faker();
        
        [Fact]
        public async Task GetRequest_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking());
            
            // Act
            await PostTrackedRequestAsync(headerName, headerValue, body);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
            Assert.DoesNotContain(RequestBodyKey, eventContext);
            Assert.DoesNotContain(ResponseBodyKey, eventContext);
        }

        [Fact]
        public async Task GetRequest_TracksRequestWithoutHeaders_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking<NoHeadersRequestTrackingMiddleware>());
           
            // Act
            await PostTrackedRequestAsync(headerName, headerValue, body);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.DoesNotContain(headerName, eventContext);
            Assert.DoesNotContain(RequestBodyKey, eventContext);
            Assert.DoesNotContain(ResponseBodyKey, eventContext);
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
            
            // Act
            await PostTrackedRequestAsync(customHeaderName, customHeaderValue, body);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.Equal(customHeaderValue, Assert.Contains(customHeaderName, eventContext));
            Assert.DoesNotContain(omittedHeaderName, eventContext);
            Assert.DoesNotContain(RequestBodyKey, eventContext);
            Assert.DoesNotContain(ResponseBodyKey, eventContext);
        }

        [Fact]
        public async Task GetRequest_TracksWithCustomOmittedHeader_ReturnsSuccess()
        {
            // Arrange
            const string customHeaderName = "x-custom-secret-header", customHeaderValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking(options => options.OmittedHeaderNames.Add(customHeaderName)));
            
            // Act
            await PostTrackedRequestAsync(customHeaderName, customHeaderValue, body);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.DoesNotContain(customHeaderName, eventContext);
            Assert.DoesNotContain(RequestBodyKey, eventContext);
            Assert.DoesNotContain(ResponseBodyKey, eventContext);
        }

        [Fact]
        public async Task GetRequestWithoutHeaders_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking(options => options.IncludeRequestHeaders = false));
            
            // Act
            await PostTrackedRequestAsync(headerName, headerValue, body);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.DoesNotContain(headerName, eventContext);
            Assert.DoesNotContain(RequestBodyKey, eventContext);
            Assert.DoesNotContain(ResponseBodyKey, eventContext);
        }

        [Fact]
        public async Task PostRequestWithRequestBody_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "echo me";
            _testServer.AddConfigure(app => app.UseRequestTracking(options => options.IncludeRequestBody = true));
            
            // Act
            await PostTrackedRequestAsync(headerName, headerValue, body);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
            Assert.Equal(body, Assert.Contains(RequestBodyKey, eventContext));
            Assert.DoesNotContain(ResponseBodyKey, eventContext);
        }

        [Fact]
        public async Task PostRequestWithRequestBody_TracksRequestTillBufferMax_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-cutom-header-{Guid.NewGuid()}",
                   headerValue = $"header-{Guid.NewGuid()}",
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            _testServer.AddConfigure(app => app.UseRequestTracking(options =>
            {
                options.IncludeRequestBody = true;
                options.RequestBodyBufferSize = 100;
            }));
            
            // Act
            await PostTrackedRequestAsync(headerName, headerValue, requestBody);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
            string partyRequestBody = Assert.Contains(RequestBodyKey, eventContext);
            Assert.StartsWith(partyRequestBody, requestBody);
            Assert.DoesNotContain(ResponseBodyKey, eventContext);
        }

        [Fact]
        public async Task PostWithWithResponseBody_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{Guid.NewGuid()}";
            _testServer.AddConfigure(app => app.UseRequestTracking(options =>
            {
                options.IncludeResponseBody = true;
            }));

            // Act
            await PostTrackedRequestAsync(headerName, headerValue, requestBody);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
            Assert.DoesNotContain(RequestBodyKey, eventContext);
            Assert.Equal(requestBody, Assert.Contains(ResponseBodyKey, eventContext));
        }

        [Fact]
        public async Task PostWithResponseBodyOverBuffer_TracksRequestTillBufferMax_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            _testServer.AddConfigure(app => app.UseRequestTracking(options =>
            {
                options.IncludeResponseBody = true;
                options.ResponseBodyBufferSize = 100;
            }));
            
            // Act
            await PostTrackedRequestAsync(headerName, headerValue, requestBody);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
            Assert.DoesNotContain(RequestBodyKey, eventContext);
            string partlyResponseBody = Assert.Contains(ResponseBodyKey, eventContext);
            Assert.True(partlyResponseBody.Length < requestBody.Length, "Only a part of the response body should be tracked");
            Assert.StartsWith(partlyResponseBody, requestBody);
        }

        [Fact]
        public async Task PostWithBothRequestAndResponseBody_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   body = $"body-{Guid.NewGuid()}";
            _testServer.AddConfigure(app => app.UseRequestTracking(options =>
            {
                options.IncludeRequestBody = true;
                options.IncludeResponseBody = true;
            }));

            // Act
            await PostTrackedRequestAsync(headerName, headerValue, body);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
            Assert.Equal(body, Assert.Contains(RequestBodyKey, eventContext));
            Assert.Equal(body, Assert.Contains(ResponseBodyKey, eventContext));
        }
        
        [Fact]
        public async Task PostWithBothRequestAndResponseBodyOverBuffer_TracksRequestTillBufferMax_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            _testServer.AddConfigure(app => app.UseRequestTracking(options =>
            {
                options.IncludeRequestBody = true;
                options.RequestBodyBufferSize = 100;
                options.IncludeResponseBody = true;
                options.ResponseBodyBufferSize = 100;
            }));
            
            // Act
            await PostTrackedRequestAsync(headerName, headerValue, requestBody);
            
            // Assert
            IDictionary<string, string> eventContext = GetLoggedEventContext();
            Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
            string partyRequestBody = Assert.Contains(RequestBodyKey, eventContext);
            Assert.True(partyRequestBody.Length < requestBody.Length, "Only a part of the request body should be tracked");
            Assert.StartsWith(partyRequestBody, requestBody);
            string partlyResponseBody = Assert.Contains(ResponseBodyKey, eventContext);
            Assert.True(partlyResponseBody.Length < requestBody.Length, "Only a part of the response body should be tracked");
            Assert.StartsWith(partlyResponseBody, requestBody);
        }

        [Theory]
        [InlineData(ExcludeRequestTrackingOnMethodController.Route)]
        [InlineData(ExcludeRequestTrackingOnClassController.Route)]
        public async Task PostRequestWithExcludeAttributeOnMethod_SkipsRequestTracking_ReturnsSuccess(string route)
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value", body = "some body";
            _testServer.AddConfigure(app => app.UseRequestTracking());
            using (HttpClient client = _testServer.CreateClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, route)
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

        [Theory]
        [InlineData(ExcludeFilterRequestTrackingOnMethodController.OnlyExcludeRequestBodyRoute, false, true)]
        [InlineData(ExcludeFilterRequestTrackingOnMethodController.OnlyExcludeResponseBodyRoute, true, false)]
        public async Task PostRequestWithExcludeFilterAttributeOnMethod_OnlySkipsRequestTrackingMatchingFilter_ReturnsSuccess(
            string route,
            bool includeRequestBody,
            bool includeResponseBody)
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value";
            string body = $"body-{Guid.NewGuid()}";
            _testServer.AddConfigure(app => app.UseRequestTracking(options =>
            {
                options.IncludeRequestBody = true;
                options.IncludeResponseBody = true;
            }));

            // Act
            using (HttpResponseMessage response = await PostRequestAsync(headerName, headerValue, body, route))
            {
                // Assert
                string responseContents = await response.Content.ReadAsStringAsync();
                Assert.Equal(ExcludeFilterRequestTrackingOnMethodController.ResponsePrefix + body, responseContents);
                
                IDictionary<string, string> eventContext = GetLoggedEventContext();
                Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                Assert.True(includeRequestBody == (eventContext.TryGetValue(RequestBodyKey, out string requestBody) && requestBody == body),
                    "Excluding the request body in the attribute filter should result that there's no request body in the logged telemetry context");
                Assert.True(includeResponseBody == (eventContext.TryGetValue(ResponseBodyKey, out string responseBody) && responseBody == ExcludeFilterRequestTrackingOnMethodController.ResponsePrefix + body),
                    "Excluding the response body in the attribute filter should result that there's no response body in the logged telemetry context");
            }
        }

        [Fact]
        public async Task PostRequestWithExcludeFilterAttributeOnMethod_GetsIgnoredWhileExcludeAttributeOnClass_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value";
            string body = $"body-{Guid.NewGuid()}";
            _testServer.AddConfigure(app => app.UseRequestTracking(options =>
            {
                options.IncludeRequestBody = true;
                options.IncludeResponseBody = true;
            }));

            // Act
            using (HttpResponseMessage response = await PostRequestAsync(headerName, headerValue, body, ExcludeFilterIgnoredWhileExcludedOnClassController.Route))
            {
                // Assert
                string responseContents = await response.Content.ReadAsStringAsync();
                Assert.Equal(ExcludeFilterIgnoredWhileExcludedOnClassController.ResponsePrefix + body, responseContents);
                Assert.False(ContainsLoggedEventContext(), "No event context should be logged when the skipped request tracking attribute is applied");
            }
        }

        [Fact]
        public async Task PostRequestWithExcludedFilterAttributeOnMethod_GetsUsedWhileExcludedAttributeOnClass_ReturnsSuccess()
        {
            // Arrange
            const string headerName = "x-custom-header", headerValue = "custom header value";
            string body = $"body-{Guid.NewGuid()}";
            _testServer.AddConfigure(app => app.UseRequestTracking(options =>
            {
                options.IncludeRequestBody = true;
                options.IncludeResponseBody = true;
            }));

            // Act
            using (HttpResponseMessage response = await PostRequestAsync(headerName, headerValue, body, ExcludeFilterUsedWhileExcludedOnClassController.Route))
            {
                // Assert
                string responseContents = await response.Content.ReadAsStringAsync();
                Assert.Equal(ExcludeFilterUsedWhileExcludedOnClassController.ResponsePrefix + body, responseContents);
                IDictionary<string, string> eventContext = GetLoggedEventContext();
                Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                Assert.DoesNotContain(RequestBodyKey, eventContext);
                Assert.DoesNotContain(ResponseBodyKey, eventContext);
            }
        }

        private async Task PostTrackedRequestAsync(string headerName, string headerValue, string requestBody)
        {
            using (HttpClient client = _testServer.CreateClient())
            using (var requestContents = new StringContent($"\"{requestBody}\"", Encoding.UTF8, "application/json"))
            using (var request = new HttpRequestMessage(HttpMethod.Post, EchoController.Route)
            {
                Headers = { {headerName, headerValue} },
                Content = requestContents
            })
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContents = await response.Content.ReadAsStringAsync();
                Assert.Equal(requestBody, responseContents);
            }
        }

        private async Task<HttpResponseMessage> PostRequestAsync(string headerName, string headerValue, string requestBody, string route = EchoController.Route)
        {
            using (HttpClient client = _testServer.CreateClient())
            using (var requestContents = new StringContent($"\"{requestBody}\"", Encoding.UTF8, "application/json"))
            using (var request = new HttpRequestMessage(HttpMethod.Post, route)
            {
                Headers = { {headerName, headerValue} },
                Content = requestContents
            })
            {
                HttpResponseMessage response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                return response;
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

        private IDictionary<string, string> GetLoggedEventContext()
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = 
                _testServer.LogSink.DequeueLogEvents()
                           .SelectMany(ev => ev.Properties);

            var eventContexts = properties.Where(prop => prop.Key == ContextProperties.TelemetryContext);
            (string key, LogEventPropertyValue eventContext) = Assert.Single(eventContexts);
            var dictionaryValue = Assert.IsType<DictionaryValue>(eventContext);

            return dictionaryValue.Elements.ToDictionary(
                item => item.Key.ToStringValue(), 
                item => item.Value.ToStringValue().Trim('\\', '\"', '[', ']'));
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
