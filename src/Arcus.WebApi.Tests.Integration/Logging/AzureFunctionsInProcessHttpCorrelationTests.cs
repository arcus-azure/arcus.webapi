using System;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Core;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using Arcus.WebApi.Tests.Core;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.WebApi.Tests.Integration.Logging
{
    public class AzureFunctionsInProcessHttpCorrelationTests
    {
        private static readonly Faker BogusGenerator = new Faker();
        private static readonly ILogger<HttpCorrelation> Logger = NullLogger<HttpCorrelation>.Instance;

        [Fact]
        public async Task CorrelateRequestForW3C_WithoutTraceParent_GetsNewParent()
        {
            // Arrange
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();
            TelemetryClient client = CreateTelemetryClient(spyChannel);

            var context = new DefaultHttpContext();
            IHttpContextAccessor contextAccessor = CreateHttpContextAccessor(context);
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();
            var options = new HttpCorrelationInfoOptions();
            var correlation = new AzureFunctionsInProcessHttpCorrelation(client, options, contextAccessor, correlationAccessor, Logger);

            // Act
            using (HttpCorrelationResult result = correlation.CorrelateHttpRequest())
            using (var measurement = DurationMeasurement.Start())
            {
                // Assert
                Assert.True(result.IsSuccess);
                Assert.NotNull(result.CorrelationInfo);
                Assert.NotNull(result.CorrelationInfo.OperationId);
                Assert.NotNull(result.CorrelationInfo.TransactionId);
                Assert.Null(result.CorrelationInfo.OperationParentId);

                SimulateSqlQueryWithMicrosoftTracking();
                await SimulateServiceBusWithMicrosoftTrackingAsync();
                await SimulateEventHubsWithMicrosoftTrackingAsync();

                var config = new LoggerConfiguration()
                    .Enrich.WithCorrelationInfo(correlationAccessor)
                    .WriteTo.ApplicationInsights(spySink);

                using (var loggerProvider = new SerilogLoggerProvider(config.CreateLogger()))
                {
                    ILogger logger = loggerProvider.CreateLogger(nameof(AzureFunctionsInProcessHttpCorrelationTests));
                    logger.LogRequest(context.Request, context.Response, measurement);
                    logger.LogAzureKeyVaultDependency("http://my.vault.azure.net", "MySecret", isSuccessful: true, measurement);
                }
            }

            RequestTelemetry requestViaArcus = AssertX.GetRequestFrom(spySink.Telemetries, req => true);
            DependencyTelemetry dependencySqlViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "SQL" && dep.Context.Operation.Id == requestViaArcus.Context.Operation.Id);
            DependencyTelemetry dependencyKeyVaultViaArcus = AssertX.GetDependencyFrom(spySink.Telemetries, dep => dep.Type == "Azure key vault" && dep.Context.Operation.Id == requestViaArcus.Context.Operation.Id);
            DependencyTelemetry dependencyServiceBusViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "Azure Service Bus" && dep.Context.Operation.Id == requestViaArcus.Context.Operation.Id);
            DependencyTelemetry dependencyEventHubsViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "Queue Message | Azure Service Bus" && dep.Context.Operation.Id == requestViaArcus.Context.Operation.Id);

            Assert.Equal(requestViaArcus.Id, dependencySqlViaMicrosoft.Context.Operation.ParentId);
            Assert.Equal(requestViaArcus.Id, dependencyKeyVaultViaArcus.Context.Operation.ParentId);
            Assert.Equal(requestViaArcus.Id, dependencyServiceBusViaMicrosoft.Context.Operation.ParentId);
            Assert.Equal(requestViaArcus.Id, dependencyEventHubsViaMicrosoft.Context.Operation.ParentId);
        }

         [Fact]
        public async Task CorrelateRequestForW3C_WithTraceParent_GetsExistingParent()
        {
            // Arrange
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();
            TelemetryClient client = CreateTelemetryClient(spyChannel);

            var context = new DefaultHttpContext();
            var traceParent = TraceParent.Generate();
            context.Request.Headers.TraceParent = traceParent.ToString();

            IHttpContextAccessor contextAccessor = CreateHttpContextAccessor(context);
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();
            var options = new HttpCorrelationInfoOptions();
            var correlation = new AzureFunctionsInProcessHttpCorrelation(client, options, contextAccessor, correlationAccessor, Logger);

            // Act
            using (HttpCorrelationResult result = correlation.CorrelateHttpRequest())
            using (var measurement = DurationMeasurement.Start())
            {
                // Assert
                Assert.True(result.IsSuccess);
                Assert.NotNull(result.CorrelationInfo);
                Assert.NotNull(result.CorrelationInfo.OperationId);
                Assert.Equal(traceParent.TransactionId, result.CorrelationInfo.TransactionId);
                Assert.Equal(traceParent.OperationParentId, result.CorrelationInfo.OperationParentId);

                SimulateSqlQueryWithMicrosoftTracking();
                await SimulateServiceBusWithMicrosoftTrackingAsync();
                await SimulateEventHubsWithMicrosoftTrackingAsync();

                var config = new LoggerConfiguration()
                    .Enrich.WithCorrelationInfo(correlationAccessor)
                    .WriteTo.ApplicationInsights(spySink);

                using (var loggerProvider = new SerilogLoggerProvider(config.CreateLogger()))
                {
                    ILogger logger = loggerProvider.CreateLogger(nameof(AzureFunctionsInProcessHttpCorrelationTests));
                    logger.LogRequest(context.Request, context.Response, measurement);
                    logger.LogAzureKeyVaultDependency("http://my.vault.azure.net", "MySecret", isSuccessful: true, measurement);
                }
            }

            RequestTelemetry requestViaArcus = AssertX.GetRequestFrom(spySink.Telemetries, req => req.Context.Operation.ParentId == traceParent.OperationParentId && req.Context.Operation.Id == traceParent.TransactionId);
            DependencyTelemetry dependencySqlViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "SQL" && dep.Context.Operation.Id == requestViaArcus.Context.Operation.Id);
            DependencyTelemetry dependencyKeyVaultViaArcus = AssertX.GetDependencyFrom(spySink.Telemetries, dep => dep.Type == "Azure key vault" && dep.Context.Operation.Id == requestViaArcus.Context.Operation.Id);
            DependencyTelemetry dependencyServiceBusViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "Azure Service Bus" && dep.Context.Operation.Id == requestViaArcus.Context.Operation.Id);
            DependencyTelemetry dependencyEventHubsViaMicrosoft = AssertX.GetDependencyFrom(spyChannel.Telemetries, dep => dep.Type == "Queue Message | Azure Service Bus" && dep.Context.Operation.Id == requestViaArcus.Context.Operation.Id);

            Assert.Equal(requestViaArcus.Id, dependencySqlViaMicrosoft.Context.Operation.ParentId);
            Assert.Equal(requestViaArcus.Id, dependencyKeyVaultViaArcus.Context.Operation.ParentId);
            Assert.Equal(requestViaArcus.Id, dependencyServiceBusViaMicrosoft.Context.Operation.ParentId);
            Assert.Equal(requestViaArcus.Id, dependencyEventHubsViaMicrosoft.Context.Operation.ParentId);
        }

        private static TelemetryClient CreateTelemetryClient(InMemoryTelemetryChannel spyChannel)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IHostingEnvironment>());
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
            });
            services.Configure<TelemetryConfiguration>(conf => conf.TelemetryChannel = spyChannel);
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<TelemetryClient>();
        }

        private static IHttpContextAccessor CreateHttpContextAccessor(HttpContext context)
        {
            var contextAccessor = new HttpContextAccessor();
            var endpoint = new Uri(BogusGenerator.Internet.UrlWithPath());
            context.Request.Host = HostString.FromUriComponent(endpoint);
            context.Request.Scheme = endpoint.Scheme;
            context.Request.PathBase = endpoint.AbsolutePath;
            context.Request.Method = HttpMethods.Get;
            contextAccessor.HttpContext = context;
            
            return contextAccessor;
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

        private static async Task SimulateServiceBusWithMicrosoftTrackingAsync()
        {
            try
            {
                var message = new ServiceBusMessage();
                await using (var client = new ServiceBusClient("Endpoint=sb://something.servicebus.windows.net/;SharedAccessKeyName=something;SharedAccessKey=something=;EntityPath=something"))
                await using (ServiceBusSender sender = client.CreateSender("something"))
                {
                    await sender.SendMessageAsync(message);
                }
            }
            catch
            {
                // Ignore:
                // We only want to simulate a Service Bus connection, no need to actually set this up.
                // A failure will still result in a dependency telemetry instance that we can assert on.
            }
        }

        private static async Task SimulateEventHubsWithMicrosoftTrackingAsync()
        {
            try
            {
                var message = new EventData("something to send");
                await using (var sender = new EventHubProducerClient("Endpoint=sb://<NamespaceName>.servicebus.windows.net/;SharedAccessKeyName=<KeyName>;SharedAccessKey=<KeyValue>", "something"))
                {
                    await sender.SendAsync(new[] { message });
                }
            }
            catch
            {
                // Ignore:
                // We only want to simulate an EventHubs connection, no need to actually set this up.
                // A failure will still result in a dependency telemetry instance that we can assert on.
            }
        }
    }
}