using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Core;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Controllers;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class W3CCorrelationMiddlewareTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="W3CCorrelationMiddlewareTests" /> class.
        /// </summary>
        public W3CCorrelationMiddlewareTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task Correlate_WithMicrosoftArcusCombinationOverTwoApis_Succeeds()
        {
            var stockSpySink = new InMemoryApplicationInsightsTelemetryConverter();
            var stockChannel = new InMemoryTelemetryChannel();
            var arcusOptions = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddHttpCorrelation();
                    services.Configure((TelemetryConfiguration config) => config.TelemetryChannel = stockChannel);
                })
                .Configure(app => app.UseHttpCorrelation()
                                     .UseRequestTracking())
                .ConfigureHost(builder =>
                {
                    builder.UseSerilog((ctx, provider, config) =>
                    {
                        config.Enrich.WithHttpCorrelationInfo(provider)
                              .WriteTo.ApplicationInsights(stockSpySink);
                    });
                });

            var productChannel = new InMemoryTelemetryChannel();
            var microsoftOptions = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetry();
                    services.Configure((TelemetryConfiguration config) => config.TelemetryChannel = productChannel);

                    services.AddHttpClient("Stock API")
                            .ConfigureHttpClient(cl => cl.BaseAddress = new Uri(arcusOptions.Url));
                });
            
            await using (var arcusApp = await TestApiServer.StartNewAsync(arcusOptions, _logger))
            await using (var microsoftApp = await TestApiServer.StartNewAsync(microsoftOptions, _logger))
            {
                var request = HttpRequestBuilder.Get(MicrosoftProductController.Route);

                using (HttpResponseMessage response = await microsoftApp.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }

            RequestTelemetry requestMicrosoftEndpoint = AssertX.GetRequestFrom(productChannel.Telemetries, r => r.Url == new Uri($"{microsoftOptions.Url}{MicrosoftProductController.Route}"));
            DependencyTelemetry dependViaMicrosoftOnArcusEndpoint = AssertX.GetDependencyFrom(stockChannel.Telemetries, d => d.Name == $"GET /{ArcusStockController.Route}");
            RequestTelemetry requestArcusEndpoint = AssertX.GetRequestFrom(stockSpySink.Telemetries, r => r.Url == new Uri(arcusOptions.Url + ArcusStockController.Route));
            DependencyTelemetry dependViaArcusOnKeyVault = AssertX.GetDependencyFrom(stockSpySink.Telemetries, d => d.Type == "Azure key vault");
            DependencyTelemetry dependViaMicrosoftOnSql = AssertX.GetDependencyFrom(stockChannel.Telemetries, d => d.Type == "SQL");
            DependencyTelemetry dependViaMicrosoftOnServiceBus = AssertX.GetDependencyFrom(productChannel.Telemetries, d => d.Type == "Azure Service Bus");
            DependencyTelemetry dependViaMicrosoftOnEventHubs = AssertX.GetDependencyFrom(productChannel.Telemetries, d => d.Type == "Queue Message | Azure Service Bus");

            Assert.Equal(requestMicrosoftEndpoint.Id, dependViaMicrosoftOnArcusEndpoint.Context.Operation.ParentId);
            Assert.Equal(dependViaMicrosoftOnArcusEndpoint.Id, requestArcusEndpoint.Context.Operation.ParentId);
            Assert.Equal(requestArcusEndpoint.Id, dependViaArcusOnKeyVault.Context.Operation.ParentId);
            Assert.Equal(requestArcusEndpoint.Id, dependViaMicrosoftOnSql.Context.Operation.ParentId);
            Assert.Equal(requestArcusEndpoint.Id, dependViaMicrosoftOnServiceBus.Context.Operation.ParentId);
            Assert.Equal(requestArcusEndpoint.Id, dependViaMicrosoftOnEventHubs.Context.Operation.ParentId);
        }

        [Fact]
        public async Task Correlate_WithMicrosoftArcusCombinationOverOneApi_Succeeds()
        {
            var stockSpySink = new InMemoryApplicationInsightsTelemetryConverter();
            var stockChannel = new InMemoryTelemetryChannel();
            var arcusOptions = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddHttpCorrelation();
                    services.Configure((TelemetryConfiguration config) => config.TelemetryChannel = stockChannel);
                })
                .Configure(app => app.UseHttpCorrelation()
                                     .UseRequestTracking())
                .ConfigureHost(builder =>
                {
                    builder.UseSerilog((ctx, provider, config) =>
                    {
                        config.Enrich.WithHttpCorrelationInfo(provider)
                              .WriteTo.ApplicationInsights(stockSpySink);
                    });
                });

            await using (var arcusApp = await TestApiServer.StartNewAsync(arcusOptions, _logger))
            {
                var request = HttpRequestBuilder.Get(ArcusStockController.Route);

                using (HttpResponseMessage response = await arcusApp.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }

            RequestTelemetry requestArcusEndpoint = AssertX.GetRequestFrom(stockSpySink.Telemetries, r => r.Url == new Uri(arcusOptions.Url + ArcusStockController.Route));
            DependencyTelemetry dependViaArcusOnKeyVault = AssertX.GetDependencyFrom(stockSpySink.Telemetries, d => d.Type == "Azure key vault");
            DependencyTelemetry dependViaMicrosoftOnSql = AssertX.GetDependencyFrom(stockChannel.Telemetries, d => d.Type == "SQL");
            DependencyTelemetry dependViaMicrosoftOnServiceBus = AssertX.GetDependencyFrom(stockChannel.Telemetries, d => d.Type == "Azure Service Bus");
            DependencyTelemetry dependViaMicrosoftOnEventHubs = AssertX.GetDependencyFrom(stockChannel.Telemetries, d => d.Type == "Queue Message | Azure Service Bus");

            Assert.Equal(requestArcusEndpoint.Id, dependViaArcusOnKeyVault.Context.Operation.ParentId);
            Assert.Equal(requestArcusEndpoint.Id, dependViaMicrosoftOnSql.Context.Operation.ParentId);
            Assert.Equal(requestArcusEndpoint.Id, dependViaMicrosoftOnServiceBus.Context.Operation.ParentId);
            Assert.Equal(requestArcusEndpoint.Id, dependViaMicrosoftOnEventHubs.Context.Operation.ParentId);
        }
    }
}
