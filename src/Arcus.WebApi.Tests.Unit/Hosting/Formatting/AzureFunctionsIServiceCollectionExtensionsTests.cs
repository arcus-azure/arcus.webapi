using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Hosting.Formatting
{
    public class AzureFunctionsIServiceCollectionExtensionsTests
    {
        [Fact]
        public void ConfigureJsonFormatting_WithoutConfigureFunctions_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            var stub = new Mock<IFunctionsWorkerApplicationBuilder>();
            stub.Setup(b => b.Services).Returns(services);
            IFunctionsWorkerApplicationBuilder builder = stub.Object;

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.ConfigureJsonFormatting(configureOptions: null));
        }

        [Fact]
        public void ConfigureJsonFormatting_WithConfigureFunctions_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var stub = new Mock<IFunctionsWorkerApplicationBuilder>();
            stub.Setup(b => b.Services).Returns(services);
            IFunctionsWorkerApplicationBuilder builder = stub.Object;

            // Act
            builder.ConfigureJsonFormatting(options => options.Converters.Add(new JsonStringEnumConverter()));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var serializer = provider.GetService<JsonObjectSerializer>();
            Assert.NotNull(serializer);

            var report = new HealthReport(new ReadOnlyDictionary<string, HealthReportEntry>(
                new Dictionary<string, HealthReportEntry>
                {
                    ["test"] = new HealthReportEntry(HealthStatus.Healthy,
                        "something healthy",
                        TimeSpan.FromSeconds(5),
                        exception: null,
                        data: null,
                        tags: null)
                }), TimeSpan.FromSeconds(5));

            BinaryData data = serializer.Serialize(report, inputType: typeof(HealthReport));
            string json = data.ToString();
            Assert.Contains("Healthy", json);
        }
    }
}
