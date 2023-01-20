using System;
using System.Net;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.AzureFunctions;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions;
using Bogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    // ReSharper disable once InconsistentNaming
    public class IFunctionsWorkerApplicationBuilderExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void UseFunctionContext_WithDefault_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.UseFunctionContext();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IFunctionContextAccessor>());
            Assert.NotNull(provider.GetService<FunctionContextMiddleware>());
        }

        [Fact]
        public void UseHttpCorrelation_WithDefault_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);
            builder.Object.UseFunctionContext();

            // Act
            builder.Object.UseHttpCorrelation();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHttpCorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<AzureFunctionsHttpCorrelation>());
            Assert.NotNull(provider.GetService<AzureFunctionsCorrelationMiddleware>());
        }

        [Theory]
        [InlineData(HttpCorrelationFormat.Hierarchical)]
        [InlineData(HttpCorrelationFormat.W3C)]
        public void UseHttpCorrelation_WithCorrelationFormat_RegistersServices(HttpCorrelationFormat format)
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);
            builder.Object.UseFunctionContext();

            // Act
            builder.Object.UseHttpCorrelation(options => options.Format = format);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHttpCorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<AzureFunctionsHttpCorrelation>());
            Assert.NotNull(provider.GetService<AzureFunctionsCorrelationMiddleware>());
        }

        [Fact]
        public void UseExceptionHandling_WithDefault_RegistersService()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.UseExceptionHandling();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<AzureFunctionsExceptionHandlingMiddleware>());
        }

        [Fact]
        public void UseExceptionHandling_WithCustom_RegistersService()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.UseExceptionHandling<CustomExceptionHandlingMiddleware>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<CustomExceptionHandlingMiddleware>());
        }

        [Fact]
        public void UseRequestTracking_WithDefault_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.UseRequestTracking();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<RequestTrackingOptions>());
            Assert.NotNull(provider.GetService<AzureFunctionsRequestTrackingMiddleware>());
        }

        [Fact]
        public void UseRequestTracking_WithCustomDefault_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.UseRequestTracking<CustomRequestTrackingMiddleware>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<RequestTrackingOptions>());
            Assert.NotNull(provider.GetService<CustomRequestTrackingMiddleware>());
        }

        [Fact]
        public void UseRequestTracking_WithOptions_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            builder.Object.UseRequestTracking(opt => opt.TrackedStatusCodes.Add(statusCode));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<RequestTrackingOptions>();
            Assert.Contains(statusCode, options.TrackedStatusCodes);
            Assert.NotNull(provider.GetService<AzureFunctionsRequestTrackingMiddleware>());
        }

        [Fact]
        public void UseRequestTracking_WithCustomOptions_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);
            var statusCode = BogusGenerator.PickRandom<HttpStatusCode>();

            // Act
            builder.Object.UseRequestTracking<CustomRequestTrackingMiddleware>(opt => opt.TrackedStatusCodes.Add(statusCode));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<RequestTrackingOptions>();
            Assert.Contains(statusCode, options.TrackedStatusCodes);
            Assert.NotNull(provider.GetService<CustomRequestTrackingMiddleware>());
        }
    }
}
