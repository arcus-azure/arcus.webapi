using System;
using System.Net;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
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
using System.Data.SqlClient;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.AzureFunctions;
using Castle.Core.Configuration;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
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
            var client = new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = spyChannel });
            var accessor = new StubHttpCorrelationInfoAccessor();

            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForW3C(client, accessor);
            var traceParent = TraceParent.Generate();

            var context = TestFunctionContext.Create(
                configureHttpRequest: req => req.Headers.Add("traceparent", traceParent.ToString()),
                configureServices: services =>
                {
                    services.AddSingleton(correlation);
                    AddApplicationInsightsTelemetry(services, spyChannel);

                    services.AddSingleton<IHttpCorrelationInfoAccessor>(accessor);
                    AddSerilog(services, spySink);
                });
            var middleware = new AzureFunctionsCorrelationMiddleware();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                var requestMiddleware = new AzureFunctionsRequestTrackingMiddleware(new RequestTrackingOptions());
                await requestMiddleware.Invoke(ctx, async ct =>
                {
                    ILogger logger = ct.GetLogger<AzureFunctionsCorrelationTests>();
                    SimulateArcusKeyVaultDependencyTracking(logger);
                    SimulateSqlQueryWithMicrosoftTracking();
                    await CreateHttpAcceptedResponse(ct);
                });
            });

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.NotNull(correlationInfo.OperationId);
            Assert.Equal(traceParent.TransactionId, correlationInfo.TransactionId);
            Assert.Equal(traceParent.OperationParentId, correlationInfo.OperationParentId);

            RequestTelemetry requestViaArcus = AssertX.GetRequestFrom(spySink.Telemetries, req => req.Context.Operation.Id == correlationInfo.TransactionId);
            DependencyTelemetry dependencyViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "SQL" && dep.Context.Operation.Id == correlationInfo.TransactionId);
            DependencyTelemetry dependencyViaArcus = AssertX.GetDependencyFrom(spySink.Telemetries, dep => dep.Type == "Azure key vault" && dep.Context.Operation.Id == correlationInfo.TransactionId);

            Assert.Equal(requestViaArcus.Id, dependencyViaMicrosoft.Context.Operation.ParentId);
            Assert.Equal(requestViaArcus.Id, dependencyViaArcus.Context.Operation.ParentId);
        }

        [Fact]
        public async Task HttpCorrelationMiddlewareW3C_WithoutTraceParent_CorrelateCorrectly()
        {
             // Arrange
            var spyChannel = new InMemoryTelemetryChannel();
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var client = new TelemetryClient(new TelemetryConfiguration { TelemetryChannel = spyChannel });
            var accessor = new StubHttpCorrelationInfoAccessor();

            AzureFunctionsHttpCorrelation correlation = CreateHttpCorrelationForW3C(client, accessor);

            var context = TestFunctionContext.Create(configureServices: services =>
            {
                services.AddSingleton(correlation);
                AddApplicationInsightsTelemetry(services, spyChannel);

                services.AddSingleton<IHttpCorrelationInfoAccessor>(accessor);
                AddSerilog(services, spySink);
            });
            var middleware = new AzureFunctionsCorrelationMiddleware();

            // Act
            await middleware.Invoke(context, async ctx =>
            {
                var requestMiddleware = new AzureFunctionsRequestTrackingMiddleware(new RequestTrackingOptions());
                await requestMiddleware.Invoke(ctx, async ct =>
                {
                    ILogger logger = ct.GetLogger<AzureFunctionsCorrelationTests>();
                    SimulateArcusKeyVaultDependencyTracking(logger);
                    SimulateSqlQueryWithMicrosoftTracking();
                    await CreateHttpAcceptedResponse(ct);
                });
            });

            // Assert
            HttpResponseData response = context.GetHttpResponseData();
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            CorrelationInfo correlationInfo = accessor.GetCorrelationInfo();
            Assert.NotNull(correlationInfo.OperationId);
            Assert.NotNull(correlationInfo.TransactionId);
            Assert.Null(correlationInfo.OperationParentId);

            RequestTelemetry requestViaArcus = AssertX.GetRequestFrom(spySink.Telemetries, req => req.Context.Operation.Id == correlationInfo.TransactionId);
            DependencyTelemetry dependencyViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "SQL" && dep.Context.Operation.Id == correlationInfo.TransactionId);
            DependencyTelemetry dependencyViaArcus = AssertX.GetDependencyFrom(spySink.Telemetries, dep => dep.Type == "Azure key vault" && dep.Context.Operation.Id == correlationInfo.TransactionId);

            Assert.Equal(requestViaArcus.Id, dependencyViaMicrosoft.Context.Operation.ParentId);
            Assert.Equal(requestViaArcus.Id, dependencyViaArcus.Context.Operation.ParentId);
        }

        private static void AddSerilog(IServiceCollection services, InMemoryApplicationInsightsTelemetryConverter spySink)
        {
            services.AddLogging(logging =>
            {
                logging.Services.AddSingleton<ILoggerProvider>(provider =>
                {
                    var logger = new LoggerConfiguration()
                        .Enrich.WithHttpCorrelationInfo(provider)
                        .WriteTo.ApplicationInsights(spySink)
                        .CreateLogger();

                    return new SerilogLoggerProvider(logger);
                });
            });
        }

        private static void AddApplicationInsightsTelemetry(IServiceCollection services, InMemoryTelemetryChannel spyChannel)
        {
            services.AddApplicationInsightsTelemetry(options => options.EnableRequestTrackingTelemetryModule = false)
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

        private static void SimulateSqlQueryWithMicrosoftTracking()
        {
            try
            {
                using (var connection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Database=master"))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Orders";
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch
            {
                // Ignore:
                // We only want to simulate a SQL connection/command, no need to actually set this up.
                // A failure will still result in a dependency telemetry instance that we can assert on.
            }
        }
    }
}
