using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
using Arcus.Testing.Logging;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Tests.Integration.Controllers;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Controllers;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Bogus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public class RequestTrackingMiddlewareTests
    {
        private const string RequestBodyKey = "RequestBody",
                             ResponseBodyKey = "ResponseBody";

        private readonly ILogger _logger;
        private readonly Faker _bogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingMiddlewareTests" /> class.
        /// </summary>
        public RequestTrackingMiddlewareTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }
        
        [Fact]
        public async Task GetRequestWithInvalidEndpointFeature_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid()}", headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app =>
                {
                    app.Use((ctx, next) =>
                    {
                        ctx.Features.Set(Mock.Of<IEndpointFeature>());
                        return next();
                    });
                    app.UseRequestTracking();
                })
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            // Act
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithHeader(headerName, headerValue);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                }
            }
        }
        
        [Fact]
        public async Task GetRequest_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-cutom-header-{Guid.NewGuid()}",
                headerValue = $"header-{Guid.NewGuid()}",
                requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Fact]
        public async Task GetRequest_TracksRequestWithoutHeaders_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-cutom-header-{Guid.NewGuid()}",
                   headerValue = $"header-{Guid.NewGuid()}",
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking<NoHeadersRequestTrackingMiddleware>())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.DoesNotContain(headerName, eventContext);
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Theory]
        [InlineData("Authentication")]
        [InlineData("X-Api-Key")]
        [InlineData("X-ARR-ClientCert")]
        [InlineData("Ocp-Apim-Subscription-Key")]
        public async Task GetRequestWithDefaultOmittedHeader_TracksRequestWithoutHeader_ReturnsSuccess(string omittedHeaderName)
        {
            // Arrange
            string headerName = $"x-cutom-header-{Guid.NewGuid()}",
                   headerValue = $"header-{Guid.NewGuid()}",
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.DoesNotContain(omittedHeaderName, eventContext);
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Fact]
        public async Task GetRequest_TracksWithCustomOmittedHeader_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-cutom-header-{Guid.NewGuid()}",
                headerValue = $"header-{Guid.NewGuid()}",
                requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.OmittedHeaderNames.Add(headerName)))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.DoesNotContain(headerValue, eventContext);
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Fact]
        public async Task GetRequestWithoutHeaders_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-cutom-header-{Guid.NewGuid()}",
                headerValue = $"header-{Guid.NewGuid()}",
                requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.IncludeRequestHeaders = false))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.DoesNotContain(headerName, eventContext);
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Fact]
        public async Task PostRequestWithRequestBody_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-cutom-header-{Guid.NewGuid()}",
                headerValue = $"header-{Guid.NewGuid()}",
                requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.IncludeRequestBody = true))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.Equal(requestBody, Assert.Contains(RequestBodyKey, eventContext));
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Fact]
        public async Task PostRequestWithRequestBody_TracksRequestTillBufferMax_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-cutom-header-{Guid.NewGuid()}",
                   headerValue = $"header-{Guid.NewGuid()}",
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeRequestBody = true;
                    opt.RequestBodyBufferSize = 100;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    string partyRequestBody = Assert.Contains(RequestBodyKey, eventContext);
                    Assert.StartsWith(partyRequestBody, requestBody);
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Fact]
        public async Task PostWithWithResponseBody_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeResponseBody = true;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    Assert.Equal(requestBody, Assert.Contains(ResponseBodyKey, eventContext));
                }
            }
        }

        [Fact]
        public async Task PostWithResponseBodyOverBuffer_TracksRequestTillBufferMax_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeResponseBody = true;
                    opt.ResponseBodyBufferSize = 100;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    string partlyResponseBody = Assert.Contains(ResponseBodyKey, eventContext);
                    Assert.True(partlyResponseBody.Length < requestBody.Length, "Only a part of the response body should be tracked");
                    Assert.StartsWith(partlyResponseBody, requestBody);
                }
            }
        }

        [Fact]
        public async Task PostWithBothRequestAndResponseBody_TracksRequest_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   body = $"body-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeRequestBody = true;
                    opt.IncludeResponseBody = true;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(body);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.Equal(body, Assert.Contains(RequestBodyKey, eventContext));
                    Assert.Equal(body, Assert.Contains(ResponseBodyKey, eventContext));
                }
            }
        }
        
        [Fact]
        public async Task PostWithBothRequestAndResponseBodyOverBuffer_TracksRequestTillBufferMax_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeRequestBody = true;
                    opt.RequestBodyBufferSize = 100;
                    opt.IncludeResponseBody = true;
                    opt.ResponseBodyBufferSize = 100;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    string partyRequestBody = Assert.Contains(RequestBodyKey, eventContext);
                    Assert.True(partyRequestBody.Length < requestBody.Length, "Only a part of the request body should be tracked");
                    Assert.StartsWith(partyRequestBody, requestBody);
                    string partlyResponseBody = Assert.Contains(ResponseBodyKey, eventContext);
                    Assert.True(partlyResponseBody.Length < requestBody.Length, "Only a part of the response body should be tracked");
                    Assert.StartsWith(partlyResponseBody, requestBody);
                }
            }
        }

        [Theory]
        [InlineData(ExcludeRequestTrackingOnMethodController.Route)]
        [InlineData(ExcludeRequestTrackingOnClassController.Route)]
        public async Task PostRequestWithExcludeAttributeOnMethod_SkipsRequestTracking_ReturnsSuccess(string route)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(route)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.False(ContainsLoggedEventContext(spySink), "No event context should be logged when the skipped request tracking attribute is applied");
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
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeRequestBody = true;
                    opt.IncludeResponseBody = true;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(route)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    string responseContents = await response.Content.ReadAsStringAsync();
                    Assert.Equal(ExcludeFilterRequestTrackingOnMethodController.ResponsePrefix + requestBody, responseContents);
                
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.True(includeRequestBody == (eventContext.TryGetValue(RequestBodyKey, out string actualRequestBody) && actualRequestBody == requestBody),
                        "Excluding the request body in the attribute filter should result that there's no request body in the logged telemetry context");
                    Assert.True(includeResponseBody == (eventContext.TryGetValue(ResponseBodyKey, out string responseBody) && responseBody == ExcludeFilterRequestTrackingOnMethodController.ResponsePrefix + requestBody),
                        "Excluding the response body in the attribute filter should result that there's no response body in the logged telemetry context");
                }
            }
        }

        [Fact]
        public async Task PostRequestWithExcludeFilterAttributeOnMethod_GetsIgnoredWhileExcludeAttributeOnClass_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeRequestBody = true;
                    opt.IncludeResponseBody = true;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(ExcludeFilterIgnoredWhileExcludedOnClassController.Route)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    string responseContents = await response.Content.ReadAsStringAsync();
                    Assert.Equal(ExcludeFilterIgnoredWhileExcludedOnClassController.ResponsePrefix + requestBody, responseContents);
                    Assert.False(ContainsLoggedEventContext(spySink), "No event context should be logged when the skipped request tracking attribute is applied");
                }
            }
        }

        [Fact]
        public async Task PostRequestWithExcludedFilterAttributeOnMethod_GetsUsedWhileExcludedAttributeOnClass_ReturnsSuccess()
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeRequestBody = true;
                    opt.IncludeResponseBody = true;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(ExcludeFilterUsedWhileExcludedOnClassController.Route)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    string responseContents = await response.Content.ReadAsStringAsync();
                    Assert.Equal(ExcludeFilterUsedWhileExcludedOnClassController.ResponsePrefix + requestBody, responseContents);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Theory]
        [InlineData(HealthController.GetRoute)]
        [InlineData("/api/v1")]
        [InlineData("/api")]
        [InlineData("api")]
        public async Task RequestWithOmittedRoute_DoesntTracksRequest_ReturnsSuccess(string omittedRoute)
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.OmittedRoutes.Add(omittedRoute)))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.False(ContainsLoggedEventContext(spySink), "No event context should be logged when the omitted route is applied");
                }
            }
        }

        [Theory]
        [InlineData(EchoController.GetPostRoute)]
        [InlineData("/echo")]
        public async Task RequestWithOmittedRouteWithBody_DoesntTracksRequest_ReturnsSuccess(string omittedRoute)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.OmittedRoutes.Add(omittedRoute)))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(EchoController.GetPostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.False(ContainsLoggedEventContext(spySink), "No event context should be logged when the omitted route is applied");
                }
            }
        }

        [Theory]
        [InlineData("apiz")]
        [InlineData("/")]
        [InlineData(null)]
        [InlineData("")]
        public async Task RequestWithWrongOmittedRoute_TracksRequest_ReturnsSuccess(string omittedRoute)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.OmittedRoutes.Add(omittedRoute)))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            // Act
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithHeader(headerName, headerValue);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                }
            }
        }

        [Theory]
        [InlineData(TrackedStatusCodeOnMethodController.Route200Ok, HttpStatusCode.OK)]
        [InlineData(TrackedStatusCodeOnMethodController.Route202Accepted, HttpStatusCode.Accepted)]
        [InlineData(TrackedStatusCodeOnClassController.Route200Ok, HttpStatusCode.OK)]
        [InlineData(TrackedStatusCodeOnClassController.Route202Accepted, HttpStatusCode.Accepted)]
        public async Task PostWithResponseBodyWithinLimitedStatusCodes_TracksRequest_ReturnsSuccess(string route, HttpStatusCode trackedStatusCode)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}", 
                   requestBody = $"body-{_bogusGenerator.Random.AlphaNumeric(1000)}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt =>
                {
                    opt.IncludeRequestBody = true;
                    opt.IncludeResponseBody = true;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(route)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(trackedStatusCode, response.StatusCode);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Assert.Equal(requestBody.Replace("request", "response"), responseBody);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    Assert.Equal(requestBody, Assert.Contains(RequestBodyKey, eventContext));
                    Assert.Equal(responseBody, Assert.Contains(ResponseBodyKey, eventContext));
                }
            }
        }

        [Theory]
        [InlineData(DiscardedStatusCodeOnMethodController.Route)]
        [InlineData(DiscardedStatusCodeOnClassController.Route)]
        public async Task PostWithResponseBodyOutsideLimitedStatusCodes_DoesntTrackRequest_ReturnsSuccess(string route)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(options =>
                {
                    options.IncludeRequestBody = true;
                    options.IncludeResponseBody = true;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                HttpStatusCode responseStatusCode = _bogusGenerator.Random.Enum(exclude: HttpStatusCode.OK);
                var requestBody = ((int) responseStatusCode).ToString();
                var request = HttpRequestBuilder
                    .Post(route)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(responseStatusCode, response.StatusCode);
                    Assert.False(ContainsLoggedEventContext(spySink), 
                        "Should not contain logged event context when the status code is discarded from request tracking");
                }
            }
        }

        [Theory]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithMinMax, 200, 499)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithFixed, 200, 499)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithFixed, 501, 599)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithCombined, 200, 499)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithAll, 200, 499)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithAll, 501, 549)]
        [InlineData(TrackedClientErrorStatusCodesOnClassController.Route, 200, 399)]
        [InlineData(TrackedNotFoundStatusCodeOnClassController.Route, 200, 403)]
        [InlineData(TrackedNotFoundStatusCodeOnClassController.Route, 405, 599)]
        [InlineData(TrackedNotFoundAndClientErrorsSubsetStatusCodesOnClassController.Route, 200, 403)]
        [InlineData(TrackedNotFoundAndClientErrorsSubsetStatusCodesOnClassController.Route, 405, 449)]
        [InlineData(TrackedNotFoundAndClientErrorsSubsetStatusCodesOnClassController.Route, 500, 599)]
        public async Task PostWithResponseOutsideStatusCodeRangesAttribute_DoesntTrackRequest_ReturnsSuccess(string route, int minimum, int maximum)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                int statusCode = _bogusGenerator.Random.Int(minimum, maximum);
                var request = HttpRequestBuilder
                    .Post(route)
                    .WithHeader(headerName, headerValue)
                    .WithParameter("responseStatusCode", statusCode);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal((HttpStatusCode) statusCode, response.StatusCode);
                    Assert.False(ContainsLoggedEventContext(spySink), 
                        "Should not contain logged event context when the status code is outside the configured range from request tracking");
                }
            }
        }

        [Theory]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithMinMax, 500, 599)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithFixed, 500, 500)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithCombined, 500, 599)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithAll, 550, 599)]
        [InlineData(TrackedServerErrorStatusCodesOnMethodController.RouteWithAll, 500, 500)]
        [InlineData(TrackedClientErrorStatusCodesOnClassController.Route, 400, 499)]
        [InlineData(TrackedNotFoundStatusCodeOnClassController.Route, 404, 404)]
        [InlineData(TrackedNotFoundAndClientErrorsSubsetStatusCodesOnClassController.Route, 404, 404)]
        [InlineData(TrackedNotFoundAndClientErrorsSubsetStatusCodesOnClassController.Route, 451, 499)]
        public async Task PostWithResponseInsideStatusCodeRangesAttribute_TracksRequest_ReturnsSuccess(string route, int mimimum, int maximum)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                int statusCode = _bogusGenerator.Random.Int(mimimum, maximum);
                var request = HttpRequestBuilder
                    .Post(route)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(statusCode.ToString());

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal((HttpStatusCode) statusCode, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                }
            }
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task PostWithResponseOutsideStatusCodesOptions_TracksRequest_ReturnsSuccess(HttpStatusCode trackedStatusCode)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.TrackedStatusCodes.Add(trackedStatusCode)))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var responseStatusCode = _bogusGenerator.Random.Enum(trackedStatusCode);
                var request = HttpRequestBuilder
                    .Post(StubbedStatusCodeController.PostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(((int) responseStatusCode).ToString());

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.False(ContainsLoggedEventContext(spySink), 
                        "Should not contain logged event context when the status code is outside the configured range from request tracking");
                }
            }
        }
        
        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task PostWithResponseInsideStatusCodesOptions_TracksRequest_ReturnsSuccess(HttpStatusCode trackedStatusCode)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.TrackedStatusCodes.Add(trackedStatusCode)))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(StubbedStatusCodeController.PostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(((int) trackedStatusCode).ToString());

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(trackedStatusCode, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                }
            }
        }
        
        [Theory]
        [InlineData(500, 599, 200)]
        [InlineData(450, 599, 315)]
        [InlineData(100, 598, 599)]
        [InlineData(200, 399, 500)]
        public async Task PostWithResponseOutsideStatusCodeRangesOptions_TracksRequest_ReturnsSuccess(int minimumThreshold, int maximumThreshold, int responseStatusCode)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.TrackedStatusCodeRanges.Add(new StatusCodeRange(minimumThreshold, maximumThreshold))))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(StubbedStatusCodeController.PostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(responseStatusCode.ToString());
                
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal((HttpStatusCode) responseStatusCode, response.StatusCode);
                    Assert.False(ContainsLoggedEventContext(spySink), 
                        "Should not contain logged event context when the status code is outside the configured range from request tracking");
                }
            }
        }

        [Theory]
        [InlineData(500, 599, 550)]
        [InlineData(500, 500, 500)]
        [InlineData(200, 399, 202)]
        [InlineData(100, 599, 315)]
        public async Task PostWithResponseInsideStatusCodeRangesOptions_TracksRequest_ReturnsSuccess(int minimumThreshold, int maximumThreshold, int responseStatusCode)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.TrackedStatusCodeRanges.Add(new StatusCodeRange(minimumThreshold, maximumThreshold))))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(StubbedStatusCodeController.PostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(responseStatusCode.ToString());

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    Assert.Equal((HttpStatusCode) responseStatusCode, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                }
            }
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.OK)]
        public async Task PostWithResponseNullStatusCodeRangeOptions_TracksAllRequest_ReturnsSuccess(HttpStatusCode responseStatusCode)
        {
            // Arrange
            string headerName = $"x-custom-header-{Guid.NewGuid():N}", 
                   headerValue = $"header-{Guid.NewGuid()}";
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking(opt => opt.TrackedStatusCodeRanges.Add(null)))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(StubbedStatusCodeController.PostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonBody(((int) responseStatusCode).ToString());
                
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    Assert.Equal(responseStatusCode, response.StatusCode);
                    IDictionary<string, string> eventContext = GetLoggedEventContext(spySink);
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                    
                }
            }
        }

        private static bool ContainsLoggedEventContext(InMemorySink testSink)
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = 
                testSink.DequeueLogEvents()
                        .SelectMany(ev => ev.Properties);

            var eventContexts = properties.Where(prop => prop.Key == ContextProperties.TelemetryContext);
            return eventContexts.Any();
        }

        private static IDictionary<string, string> GetLoggedEventContext(InMemorySink testSink)
        {
            IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties = 
                testSink.DequeueLogEvents()
                        .SelectMany(ev => ev.Properties);

            var eventContexts = properties.Where(prop => prop.Key == ContextProperties.TelemetryContext);
            (string key, LogEventPropertyValue eventContext) = Assert.Single(eventContexts);
            var dictionaryValue = Assert.IsType<DictionaryValue>(eventContext);

            return dictionaryValue.Elements.ToDictionary(
                item => item.Key.ToStringValue(), 
                item => item.Value.ToStringValue().Trim('\\', '\"', '[', ']'));
        }
    }
}
