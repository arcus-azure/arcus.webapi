using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Core;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.AzureFunctions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Configuration;
using Serilog.Extensions.Logging;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class AzureFunctionsCorrelationTests
    {
        [Fact]
        public async Task HttpCorrelationMiddlewareW3C_WithTraceParent_CorrelateCorrectly()
        {
            // Arrange
            var spyChannel = new InMemoryTelemetryChannel();
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();
            
            using (TestFunctionContext context = CreateFunctionContextWithApplicationInsights(correlationAccessor, spyChannel, spySink))
            {
                HttpRequestData request = await context.GetHttpRequestDataAsync();
                var traceParent = TraceParent.Generate();
                request.Headers.Add("traceparent", traceParent.ToString());
                var middleware = new AzureFunctionsCorrelationMiddleware();
                
                // Act
                await middleware.Invoke(context, async ctx =>
                {
                    var requestMiddleware = new AzureFunctionsRequestTrackingMiddleware(new RequestTrackingOptions());
                    await requestMiddleware.Invoke(ctx, async ct =>
                    {
                        ILogger logger = ct.GetLogger<AzureFunctionsCorrelationTests>();
                        SimulateArcusKeyVaultDependencyTracking(logger);
                        await SimulateHttpWithMicrosoftTrackingAsync();
                        await CreateHttpAcceptedResponse(ct);
                    });
                });

                // Assert
                HttpResponseData response = context.GetHttpResponseData();
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                
                CorrelationInfo correlationInfo = correlationAccessor.GetCorrelationInfo();
                Assert.NotNull(correlationInfo.OperationId);
                Assert.Equal(traceParent.TransactionId, correlationInfo.TransactionId);
                Assert.Equal(traceParent.OperationParentId, correlationInfo.OperationParentId);

                RequestTelemetry requestViaArcus = AssertX.GetRequestFrom(spySink.Telemetries, req => req.Context.Operation.Id == correlationInfo.TransactionId);
                DependencyTelemetry dependencyViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "Http" && dep.Context.Operation.Id == correlationInfo.TransactionId);
                DependencyTelemetry dependencyViaArcus = AssertX.GetDependencyFrom(spySink.Telemetries, dep => dep.Type == "Azure key vault" && dep.Context.Operation.Id == correlationInfo.TransactionId);
                Assert.Equal(requestViaArcus.Id, dependencyViaMicrosoft.Context.Operation.ParentId);
                Assert.Equal(requestViaArcus.Id, dependencyViaArcus.Context.Operation.ParentId);
            }
        }

        [Fact]
        public async Task HttpCorrelationMiddlewareW3C_WithoutTraceParent_CorrelateCorrectly()
        {
            // Arrange
            var spyChannel = new InMemoryTelemetryChannel();
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();

            using (TestFunctionContext context = CreateFunctionContextWithApplicationInsights(correlationAccessor, spyChannel, spySink))
            {
                HttpRequestData request = await context.GetHttpRequestDataAsync();
                request.Headers.Remove("traceparent");
                var middleware = new AzureFunctionsCorrelationMiddleware();

                // Act
                await middleware.Invoke(context, async ctx =>
                {
                    var requestMiddleware = new AzureFunctionsRequestTrackingMiddleware(new RequestTrackingOptions());
                    await requestMiddleware.Invoke(ctx, async ct =>
                    {
                        ILogger logger = ct.GetLogger<AzureFunctionsCorrelationTests>();
                        SimulateArcusKeyVaultDependencyTracking(logger);
                        await SimulateHttpWithMicrosoftTrackingAsync();
                        await CreateHttpAcceptedResponse(ct);
                    });
                });

                // Assert
                HttpResponseData response = context.GetHttpResponseData();
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                CorrelationInfo correlationInfo = correlationAccessor.GetCorrelationInfo();
                Assert.NotNull(correlationInfo.OperationId);
                Assert.NotNull(correlationInfo.TransactionId);
                Assert.Null(correlationInfo.OperationParentId);

                RequestTelemetry requestViaArcus = AssertX.GetRequestFrom(spySink.Telemetries, req => req.Context.Operation.Id == correlationInfo.TransactionId);
                DependencyTelemetry dependencyViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "Http" && dep.Context.Operation.Id == correlationInfo.TransactionId);
                DependencyTelemetry dependencyViaArcus = AssertX.GetDependencyFrom(spySink.Telemetries, dep => dep.Type == "Azure key vault" && dep.Context.Operation.Id == correlationInfo.TransactionId);
                Assert.Equal(requestViaArcus.Id, dependencyViaMicrosoft.Context.Operation.ParentId);
                Assert.Equal(requestViaArcus.Id, dependencyViaArcus.Context.Operation.ParentId);
            }
        }

        private static TestFunctionContext CreateFunctionContextWithApplicationInsights(
            StubHttpCorrelationInfoAccessor correlationAccessor, 
            InMemoryTelemetryChannel spyChannel, 
            InMemoryApplicationInsightsTelemetryConverter spySink)
        {
            return TestFunctionContext.Create(configureServices: services =>
            {
                services.AddSingleton(provider =>
                {
                    var client = provider.GetRequiredService<TelemetryClient>();
                    return CreateHttpCorrelationForW3C(client, correlationAccessor);
                });
                AddApplicationInsightsTelemetry(services, spyChannel);

                services.AddSingleton<IHttpCorrelationInfoAccessor>(correlationAccessor);
                AddSerilog(services, spySink);
            });
        }

        private static void AddSerilog(IServiceCollection services, InMemoryApplicationInsightsTelemetryConverter spySink)
        {
            services.AddLogging(logging =>
            {
                logging.Services.AddSingleton<ILoggerProvider>(provider =>
                {
                    var logger = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .Enrich.WithHttpCorrelationInfo(provider)
                        .WriteTo.ApplicationInsights(spySink)
                        .CreateLogger();

                    return new SerilogLoggerProvider(logger);
                });
            });
        }

        private static void AddApplicationInsightsTelemetry(IServiceCollection services, InMemoryTelemetryChannel spyChannel)
        {
            services.AddApplicationInsightsTelemetry(ai =>
                    {
                        ai.EnableAdaptiveSampling = false;
                        ai.AddAutoCollectedMetricExtractor = false;
                        ai.EnableEventCounterCollectionModule = false;
                        ai.EnableDiagnosticsTelemetryModule = false;
                        ai.EnablePerformanceCounterCollectionModule = false;
                        ai.EnableQuickPulseMetricStream = false;
                        ai.InstrumentationKey = "ikey";
                        ai.DeveloperMode = true;
                    })
                    .Configure((TelemetryConfiguration config) => config.TelemetryChannel = spyChannel);

            services.AddSingleton(Mock.Of<IHostingEnvironment>());
            services.AddSingleton<IConfiguration>(new ConfigurationManager());
        }

        private static AzureFunctionsHttpCorrelation CreateHttpCorrelationForW3C(
            TelemetryClient client,
            IHttpCorrelationInfoAccessor accessor,
            Action<HttpCorrelationInfoOptions> configureOptions = null)
        {
            var options = new HttpCorrelationInfoOptions();
            configureOptions?.Invoke(options);
            options.Format = HttpCorrelationFormat.W3C;

            var correlation = new AzureFunctionsHttpCorrelation(client, options, accessor, NullLogger<AzureFunctionsHttpCorrelation>.Instance);
            return correlation;
        }

        private static async Task CreateHttpAcceptedResponse(FunctionContext context)
        {
            HttpRequestData request = await context.GetHttpRequestDataAsync();
            context.GetInvocationResult().Value = request.CreateResponse(HttpStatusCode.Accepted);
        }

        private static void SimulateArcusKeyVaultDependencyTracking(ILogger logger)
        {
            logger.LogAzureKeyVaultDependency("https://my-vault.azure.net", "Sql-connection-string", isSuccessful: true, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        }

        private static async Task SimulateHttpWithMicrosoftTrackingAsync()
        {
            try
            {
                var client = new HttpClient();
                await client.GetStringAsync("http://non-exsiting-endpoint");
            }
            catch
            {
                // Ignore:
                // We only want to simulate a HTTP connection, no need to actually set this up.
                // A failure will still result in a dependency telemetry instance that we can assert on.
            }
        }
    }
}
