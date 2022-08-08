using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Logging.Controllers;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

#pragma warning disable 618 // disable obsolete warnings (for now) regarding the '.AddHttpCorrelation' extension.

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class TelemetryCorrelationTests
    {
        private const string TransactionIdPropertyName = "TransactionId",
                             OperationIdPropertyName = "OperationId";

        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryCorrelationTests" /> class.
        /// </summary>
        public TelemetryCorrelationTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }
        
        [Fact]
        public async Task SendRequest_WithSerilogCorrelationEnrichment_ReturnsOkWithEnrichedCorrelationLogProperties()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation())
                .PreConfigure(app => app.UseHttpCorrelation())
                .ConfigureHost(host => host.UseSerilog((context, serviceProvider, config) =>
                    config.Enrich.WithHttpCorrelationInfo(serviceProvider)
                          .WriteTo.Sink(spySink)));
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(CorrelationController.GetRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    CorrelationInfo correlationInfo = await AssertAppCorrelationInfoAsync(response);
                    AssertLoggedCorrelationProperties(spySink, correlationInfo);
                }
            }
        }

        [Fact]
        public async Task SendRequest_WithSerilogCorrelationenrichment_ReturnsOkWithDifferentOperationIdAndSameTransactionId()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation())
                .PreConfigure(app => app.UseHttpCorrelation())
                .ConfigureHost(host => host.UseSerilog((context, serviceProvider, config) =>
                    config.Enrich.WithHttpCorrelationInfo(serviceProvider)
                          .WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var firstRequest = HttpRequestBuilder.Get(CorrelationController.GetRoute);
                using (HttpResponseMessage firstResponse = await server.SendAsync(firstRequest))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
                    CorrelationInfo firstCorrelationInfo = await AssertAppCorrelationInfoAsync(firstResponse);
                    AssertLoggedCorrelationProperties(spySink, firstCorrelationInfo);

                    var secondRequest = HttpRequestBuilder
                        .Get(CorrelationController.GetRoute)
                        .WithHeader("X-Transaction-ID", firstCorrelationInfo.TransactionId);

                    using (HttpResponseMessage secondResponse = await server.SendAsync(secondRequest))
                    {
                        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
                        CorrelationInfo secondCorrelationInfo = await AssertAppCorrelationInfoAsync(secondResponse);
                        AssertLoggedCorrelationProperties(spySink, secondCorrelationInfo);

                        Assert.NotEqual(firstCorrelationInfo.OperationId, secondCorrelationInfo.OperationId);
                        Assert.Equal(firstCorrelationInfo.TransactionId, secondCorrelationInfo.TransactionId);
                    }
                }
            }
        }
        
        [Fact]
        public async Task SendRequest_WithSerilogCorrelationenrichment_ReturnsOkWithDifferentOperationIdAndDifferentTransactionId()
        {
            // Arrange
            var spySink = new InMemorySink();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddHttpCorrelation())
                .PreConfigure(app => app.UseHttpCorrelation())
                .ConfigureHost(host => host.UseSerilog((context, serviceProvider, config) =>
                    config.Enrich.WithHttpCorrelationInfo(serviceProvider)
                          .WriteTo.Sink(spySink)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Act
                var request = HttpRequestBuilder.Get(CorrelationController.GetRoute);
                using (HttpResponseMessage firstResponse = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
                    CorrelationInfo firstCorrelationInfo = await AssertAppCorrelationInfoAsync(firstResponse);
                    AssertLoggedCorrelationProperties(spySink, firstCorrelationInfo);

                    using (HttpResponseMessage secondResponse = await server.SendAsync(request))
                    {
                        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
                        CorrelationInfo secondCorrelationInfo = await AssertAppCorrelationInfoAsync(secondResponse);
                        AssertLoggedCorrelationProperties(spySink, secondCorrelationInfo);
                    
                        Assert.NotEqual(firstCorrelationInfo.OperationId, secondCorrelationInfo.OperationId);
                        Assert.NotEqual(firstCorrelationInfo.TransactionId, secondCorrelationInfo.TransactionId);
                    }
                }
            }
        }

        private static async Task<CorrelationInfo> AssertAppCorrelationInfoAsync(HttpResponseMessage response)
        {
            string json = await response.Content.ReadAsStringAsync();
            var content = JsonConvert.DeserializeAnonymousType(json, new { TransactionId = "", OperationId = "" });
            Assert.False(String.IsNullOrWhiteSpace(content.TransactionId), "Accessed 'X-Transaction-ID' cannot be blank");
            Assert.False(String.IsNullOrWhiteSpace(content.OperationId), "Accessed 'X-Operation-ID' cannot be blank");

            return new CorrelationInfo(content.OperationId, content.TransactionId);
        }

        private static void AssertLoggedCorrelationProperties(InMemorySink testSink, CorrelationInfo correlationInfo)
        {
            KeyValuePair<string, LogEventPropertyValue>[] properties = 
                testSink.DequeueLogEvents()
                        .SelectMany(ev => ev.Properties)
                        .ToArray();

            Assert.Contains(
                properties.Where(prop => prop.Key == TransactionIdPropertyName), 
                prop => correlationInfo.TransactionId == prop.Value.ToStringValue());
            
            Assert.Contains(
                properties.Where(prop => prop.Key == OperationIdPropertyName), 
                prop => correlationInfo.OperationId == prop.Value.ToStringValue());
        }
    }
}
