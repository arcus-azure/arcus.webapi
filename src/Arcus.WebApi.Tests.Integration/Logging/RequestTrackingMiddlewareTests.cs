using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.Core.Correlation;
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
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class RequestTrackingMiddlewareTests
    {
        private const string RequestBodyKey = "RequestBody",
                             ResponseBodyKey = "ResponseBody",
                             OperationNameKey = "OperationName";

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
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
                    Assert.DoesNotContain(headerName, eventContext);
                    Assert.DoesNotContain(RequestBodyKey, eventContext);
                    Assert.DoesNotContain(ResponseBodyKey, eventContext);
                }
            }
        }

        [Fact]
        public async Task PostOrder_TracksRequestWithoutClientId_ReturnsSuccess()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking<RemoveClientIdFromOrderRequestTrackingMiddleware>(opt =>
                {
                    opt.IncludeRequestBody = true;
                    opt.IncludeResponseBody = true;
                }))
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = _bogusGenerator.Commerce.Ean13(),
                ClientId = _bogusGenerator.Person.Email,
                Scheduled = _bogusGenerator.Date.RecentOffset()
            };
            string json = JsonSerializer.Serialize(order);

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(OrderController.PostRoute)
                    .WithJsonBody(json);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();

                    (string requestBodyKey, string requestBody) = Assert.Single(eventContext, item => item.Key == RequestBodyKey);
                    var actualTrackedRequestBody = JsonSerializer.Deserialize<Order>(requestBody.Replace("\\", ""));
                    Assert.Equal(order.Id, actualTrackedRequestBody.Id);
                    Assert.Equal(order.ArticleNumber, actualTrackedRequestBody.ArticleNumber);
                    Assert.Null(actualTrackedRequestBody.ClientId);
                    Assert.Equal(order.Scheduled, actualTrackedRequestBody.Scheduled);

                    (string responseBodyKey, string responseBody) = Assert.Single(eventContext, item => item.Key == ResponseBodyKey);
                    var actualTrackedResponseBody = JsonSerializer.Deserialize<Order>(responseBody.Replace("\\", ""));
                    Assert.Null(actualTrackedResponseBody.Id);
                    Assert.Equal(order.ArticleNumber, actualTrackedResponseBody.ArticleNumber);
                    Assert.Null(actualTrackedResponseBody.ClientId);
                    Assert.Equal(order.Scheduled, actualTrackedResponseBody.Scheduled);
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(body);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.False(spySink.HasRequestLogProperties(), "No event context should be logged when the skipped request tracking attribute is applied");
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    string responseContents = await response.Content.ReadAsStringAsync();
                    Assert.Equal(ExcludeFilterRequestTrackingOnMethodController.ResponsePrefix + requestBody, responseContents);

                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    string responseContents = await response.Content.ReadAsStringAsync();
                    Assert.Equal(ExcludeFilterIgnoredWhileExcludedOnClassController.ResponsePrefix + requestBody, responseContents);
                    Assert.False(spySink.HasRequestLogProperties(), "No event context should be logged when the skipped request tracking attribute is applied");
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    string responseContents = await response.Content.ReadAsStringAsync();
                    Assert.Equal(ExcludeFilterUsedWhileExcludedOnClassController.ResponsePrefix + requestBody, responseContents);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    Assert.False(spySink.HasRequestLogProperties(), "No event context should be logged when the omitted route is applied");
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
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.False(spySink.HasRequestLogProperties(), "No event context should be logged when the omitted route is applied");
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
                    .WithHeader(headerName, headerValue)
                    .WithHeader(HttpCorrelationProperties.UpstreamServiceHeaderName, null);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithHeader(HttpCorrelationProperties.UpstreamServiceHeaderName, null)
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(trackedStatusCode, response.StatusCode);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Assert.Equal(requestBody.Replace("request", "response"), responseBody);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                int responseStatusCode = _bogusGenerator.Random.Int(201, 599);
                var requestBody = responseStatusCode.ToString();
                var request = HttpRequestBuilder
                    .Post(route)
                    .WithHeader(headerName, headerValue)
                    .WithJsonText(requestBody);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(responseStatusCode, (int)response.StatusCode);
                    Assert.False(spySink.HasRequestLogProperties(),
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
                    Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
                    Assert.False(spySink.HasRequestLogProperties(),
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
                    .WithParameter("responseStatusCode", statusCode);

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                var responseStatusCode = _bogusGenerator.Random.Int(201, 403);
                var request = HttpRequestBuilder
                    .Post(StubbedStatusCodeController.PostRoute)
                    .WithHeader(headerName, headerValue)
                    .WithJsonText(((int)responseStatusCode).ToString());

                using (HttpResponseMessage _ = await server.SendAsync(request))
                {
                    // Assert
                    Assert.False(spySink.HasRequestLogProperties(),
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
                    .WithJsonText(((int)trackedStatusCode).ToString());

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(trackedStatusCode, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));
                }
            }
        }

        [Theory]
        [InlineData(500, 599, 200)]
        [InlineData(450, 599, 315)]
        [InlineData(200, 598, 599)]
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
                    .WithJsonText(responseStatusCode.ToString());

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal((HttpStatusCode)responseStatusCode, response.StatusCode);
                    Assert.False(spySink.HasRequestLogProperties(),
                        "Should not contain logged event context when the status code is outside the configured range from request tracking");
                }
            }
        }

        [Theory]
        [InlineData(500, 599, 550)]
        [InlineData(500, 500, 500)]
        [InlineData(200, 399, 202)]
        [InlineData(200, 599, 315)]
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
                    .WithJsonText(responseStatusCode.ToString());

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    Assert.Equal((HttpStatusCode)responseStatusCode, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
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
                    .WithJsonText(((int)responseStatusCode).ToString());

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    Assert.Equal(responseStatusCode, response.StatusCode);
                    IDictionary<string, string> eventContext = spySink.GetLoggedEventContext();
                    Assert.Equal(headerValue, Assert.Contains(headerName, eventContext));

                }
            }
        }

        [Fact]
        public async Task GetRequest_TracksRequest_CorrectOperationNameIsLogged()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(EchoController.GetPostRoute);
                
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var requestLogProperties = spySink.GetRequestLogProperties();

                    Assert.Equal("GET echo", Assert.Contains(OperationNameKey, requestLogProperties).ToDecentString());
                }
            }
        }

        [Fact]
        public async Task PostRequestWithRouteParameters_TracksRequest_CorrectOperationNameIsLogged()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .Configure(app => app.UseRequestTracking())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Post(RequestOperationNameController.GetPostRouteWithRouteParameters(deviceId: 17))
                    .WithJsonText("someBody");

                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var requestLogProperties = spySink.GetRequestLogProperties();

                    Assert.Equal("POST devices/{deviceId}/echo", Assert.Contains(OperationNameKey, requestLogProperties).ToDecentString());
                }
            }
        }

        //private static bool ContainsLoggedEventContext(InMemorySink testSink)
        //{
        //    return testSink.HasRequestLogProperties();
        //}
    }
}