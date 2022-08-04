using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Testing.Logging;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Controllers;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection("Integration")]
    public class HttpCorrelationMessageHandlerTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelationMessageHandlerTests" /> class.
        /// </summary>
        public HttpCorrelationMessageHandlerTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Theory]
        [InlineData(ServiceAController.RouteWithMessageHandler)]
        [InlineData(ServiceAController.RouteWithExtension)]
        public async Task WithHtpCorrelationTracking_WithinHttpContext_Succeeds(string routeToServiceA)
        {
            // Arrange
            string dependencyIdAtServiceB = null;
            string generatedTransactionId = null;
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddHttpAssert("service-a", context =>
                    {
                        CorrelationInfo correlation = AssertCorrelationServiceA(context);
                        generatedTransactionId = correlation.TransactionId;
                    });
                    services.AddHttpAssert("service-b", context =>
                    {
                        dependencyIdAtServiceB = AssertUpstreamServiceHeader(context);
                        AssertHeaderValue(context, HttpCorrelationProperties.TransactionIdHeaderName, generatedTransactionId);
                    });
                    services.AddHttpCorrelation()
                            .AddHttpClient("from-service-a-to-service-b")
                            .WithHttpCorrelationTracking();
                })
                .Configure(app => app.UseHttpCorrelation())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            HttpRequestBuilder request = CreateHttpRequestToServiceA(routeToServiceA, options);

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string dependencyIdInTelemetry = GetDependencyIdFromTrackedDependencyTelemetry(spySink);
                    Assert.Equal(dependencyIdInTelemetry, dependencyIdAtServiceB);
                }
            }
        }

        [Theory]
        [InlineData(ServiceAController.RouteWithMessageHandler)]
        [InlineData(ServiceAController.RouteWithExtension)]
        public async Task WithHtpCorrelationTrackingWithCustomDependencyId_WithinHttpContext_Succeeds(string routeToServiceA)
        {
            // Arrange
            string dependencyIdAtServiceB = null;
            string transactionId = null;
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddHttpAssert("service-a", context =>
                    {
                        CorrelationInfo correlation = AssertCorrelationServiceA(context);
                        transactionId = correlation.TransactionId;
                    });
                    services.AddHttpAssert("service-b", context =>
                    {
                        dependencyIdAtServiceB = AssertUpstreamServiceHeader(context);
                        AssertHeaderValue(context, HttpCorrelationProperties.TransactionIdHeaderName, transactionId);
                    });
                    services.AddHttpCorrelation()
                            .AddHttpClient("from-service-a-to-service-b")
                            .WithHttpCorrelationTracking();
                })
                .Configure(app => app.UseHttpCorrelation())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            HttpRequestBuilder request = CreateHttpRequestToServiceA(routeToServiceA, options);

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string dependencyIdInTelemetry = GetDependencyIdFromTrackedDependencyTelemetry(spySink);
                    Assert.Equal(dependencyIdInTelemetry, dependencyIdAtServiceB);
                }
            }
        }

        [Theory]
        [InlineData(HttpCorrelationProperties.TransactionIdHeaderName, HttpCorrelationProperties.UpstreamServiceHeaderName)]
        [InlineData("X-MyTransaction-Id", HttpCorrelationProperties.UpstreamServiceHeaderName)]
        [InlineData(HttpCorrelationProperties.TransactionIdHeaderName, "My-Request-Id")]
        [InlineData("X-MyTransaction-Id", "X-MyRequest-Id")]
        public async Task WithHttpCorrelationTrackingWithCustomHeaders_WithinHttpContext_Succeeds(
            string transactionIdHeaderName, 
            string dependencyIdHeaderName)
        {
            // Arrange
            string expectedDependencyId = Guid.NewGuid().ToString(), expectedTransactionId = Guid.NewGuid().ToString();
            string telemetryContextKey = $"key-{Guid.NewGuid()}", telemetryContextValue = $"value-{Guid.NewGuid()}";

            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddHttpAssert("service-a", context =>
                    {
                        CorrelationInfo correlation = AssertCorrelationServiceA(context);
                        Assert.Equal(expectedTransactionId, correlation.TransactionId);
                    });
                    services.AddHttpAssert("service-b", context =>
                    {
                        AssertHeaderValue(context, dependencyIdHeaderName, expectedDependencyId);
                        AssertHeaderValue(context, transactionIdHeaderName, expectedTransactionId);
                        AssertCorrelationServiceB(context, expectedDependencyId, expectedTransactionId);
                    });
                    services.AddHttpCorrelation(options =>
                            {
                                options.Transaction.HeaderName = transactionIdHeaderName;
                                options.UpstreamService.HeaderName = dependencyIdHeaderName;
                            })
                            .AddHttpClient("from-service-a-to-service-b")
                            .WithHttpCorrelationTracking(options =>
                            {
                                options.GenerateDependencyId = () => expectedDependencyId;
                                options.TransactionIdHeaderName = transactionIdHeaderName;
                                options.UpstreamServiceHeaderName = dependencyIdHeaderName;
                                options.AddTelemetryContext(new Dictionary<string, object> { [telemetryContextKey] = telemetryContextValue });
                            });
                })
                .Configure(app => app.UseHttpCorrelation())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            HttpRequestBuilder request =
                CreateHttpRequestToServiceA(ServiceAController.RouteWithMessageHandler, options)
                    .WithHeader(transactionIdHeaderName, expectedTransactionId)
                    .WithHeader(ServiceAController.TelemetryContextKey, telemetryContextKey)
                    .WithHeader(ServiceAController.TelemetryContextValue, telemetryContextValue);

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    StructureValue trackedDependency = GetTrackedDependency(spySink);
                    Assert.Single(trackedDependency.Properties, 
                        prop => prop.Name == "Context" 
                                && prop.Value.ToStringValue().Contains(telemetryContextKey) 
                                && prop.Value.ToStringValue().Contains(telemetryContextValue));

                    string actualDependencyId = GetDependencyIdFromTrackedDependency(trackedDependency);
                    Assert.Equal(expectedDependencyId, actualDependencyId);

                }
            }
        }

        [Theory]
        [InlineData(HttpCorrelationProperties.TransactionIdHeaderName, HttpCorrelationProperties.UpstreamServiceHeaderName)]
        [InlineData("X-MyTransaction-Id", HttpCorrelationProperties.UpstreamServiceHeaderName)]
        [InlineData(HttpCorrelationProperties.TransactionIdHeaderName, "My-Request-Id")]
        [InlineData("X-MyTransaction-Id", "X-MyRequest-Id")]
        public async Task WithHttpCorrelationTrackingViaExtensionWithCustomHeaders_WithinHttpContext_Succeeds(
            string transactionIdHeaderName, 
            string dependencyIdHeaderName)
        {
            // Arrange
            var expectedDependencyId = Guid.NewGuid().ToString();
            var expectedTransactionId = Guid.NewGuid().ToString();

            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddHttpAssert("service-a", context =>
                    {
                        CorrelationInfo correlation = AssertCorrelationServiceA(context);
                        Assert.Equal(expectedTransactionId, correlation.TransactionId);
                    });
                    services.AddHttpAssert("service-b", context =>
                    {
                        AssertHeaderValue(context, dependencyIdHeaderName, expectedDependencyId);
                        AssertHeaderValue(context, transactionIdHeaderName, expectedTransactionId);
                        AssertCorrelationServiceB(context, expectedDependencyId, expectedTransactionId);
                    });
                    services.AddHttpClient("from-service-a-to-service-b");
                    services.AddHttpCorrelation(options =>
                    {
                        options.Transaction.HeaderName = transactionIdHeaderName;
                        options.UpstreamService.HeaderName = dependencyIdHeaderName;
                    });
                })
                .Configure(app => app.UseHttpCorrelation())
                .ConfigureHost(host => host.UseSerilog((context, config) => config.WriteTo.Sink(spySink)));

            HttpRequestBuilder request = 
                CreateHttpRequestToServiceA(ServiceAController.RouteWithExtension, options)
                    .WithHeader(transactionIdHeaderName, expectedTransactionId)
                    .WithHeader(ServiceAController.DependencyIdGenerationParameter, expectedDependencyId)
                    .WithHeader(ServiceAController.DependencyIdHeaderNameParameter, dependencyIdHeaderName)
                    .WithHeader(ServiceAController.TransactionIdHeaderNameParameter, transactionIdHeaderName);

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string actualDependencyId = GetDependencyIdFromTrackedDependencyTelemetry(spySink);
                    Assert.Equal(expectedDependencyId, actualDependencyId);
                }
            }
        }

        private HttpRequestBuilder CreateHttpRequestToServiceA(string routeToServiceA, TestApiServerOptions options)
        {
            var request = HttpRequestBuilder
                .Get(routeToServiceA)
                .WithHeader(ServiceAController.ServiceBUrlParameterName, options.Url + ServiceBController.Route);
            
            return request;
        }

        private static CorrelationInfo AssertCorrelationServiceA(HttpContext context)
        {
            var correlation = context.Features.Get<CorrelationInfo>();
            Assert.NotNull(correlation);
            Assert.True(correlation.OperationParentId is null, "Operation parent ID at service A should be blank");
            Assert.False(string.IsNullOrWhiteSpace(correlation.TransactionId), "Transaction ID at service A should not be blank");
            Assert.False(string.IsNullOrWhiteSpace(correlation.OperationId), "Operation ID at service A should not be blank");

            return correlation;
        }

        private static void AssertCorrelationServiceB(HttpContext context, string operationParentId, string transactionId)
        {
            var correlation = context.Features.Get<CorrelationInfo>();
            Assert.NotNull(correlation);
            Assert.False(string.IsNullOrWhiteSpace(correlation.OperationParentId), "Operation parent ID at service B should not be blank");
            Assert.False(string.IsNullOrWhiteSpace(correlation.TransactionId), "Transaction ID at service B should not be blank");
            Assert.False(string.IsNullOrWhiteSpace(correlation.OperationId), "Operation ID at service B should not be blank");

            Assert.Equal(operationParentId, correlation.OperationParentId);
            Assert.Equal(transactionId, correlation.TransactionId);
        }

        private static string AssertUpstreamServiceHeader(HttpContext context)
        {
            return AssertHeaderAvailable(context, HttpCorrelationProperties.UpstreamServiceHeaderName);
        }

        private static void AssertHeaderValue(
            HttpContext context,
            string headerName,
            string expected)
        {
            string actual = AssertHeaderAvailable(context, headerName);
            Assert.Equal(expected, actual);
        }

        private static string AssertHeaderAvailable(HttpContext context, string headerName)
        {
            string headerValue = context.Request.Headers[headerName];
            Assert.False(string.IsNullOrWhiteSpace(headerValue), $"HTTP request header '{headerName}' only contains a blank header value");

            return headerValue;
        }

        private static string GetDependencyIdFromTrackedDependencyTelemetry(InMemorySink spySink)
        {
            StructureValue dependency = GetTrackedDependency(spySink);

            string dependencyId = GetDependencyIdFromTrackedDependency(dependency);
            return dependencyId;
        }

        private static string GetDependencyIdFromTrackedDependency(StructureValue trackedDependency)
        {
            string actualDependencyId = Assert.Single(trackedDependency.Properties, prop => prop.Name == "DependencyId").Value.ToStringValue();
            Assert.False(string.IsNullOrWhiteSpace(actualDependencyId), "Logged dependency ID only contains blank value");

            return actualDependencyId;
        }

        private static StructureValue GetTrackedDependency(InMemorySink spySink)
        {
            LogEvent[] logEvents = spySink.DequeueLogEvents().ToArray();
            Assert.NotEmpty(logEvents);

            LogEvent dependencyLogEvent = Assert.Single(logEvents, ev => ev.MessageTemplate.Text == "{@Dependency}");
            LogEventPropertyValue dependencyProperty = Assert.Contains("Dependency", dependencyLogEvent.Properties);
            var dependency = Assert.IsType<StructureValue>(dependencyProperty);
            Assert.Contains(dependency.Properties, prop => prop.Name == "DependencyName" && prop.Value.ToStringValue() == "GET /service-b");

            return dependency;
        }
    }
}
