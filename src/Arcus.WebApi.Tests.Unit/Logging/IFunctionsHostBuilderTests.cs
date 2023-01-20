using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection; 
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    // ReSharper disable once InconsistentNaming
    public class IFunctionsHostBuilderTests
    {
        [Fact]
        public void AddHttpCorrelation_WithDefault_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(build => build.Services).Returns(services);

            // Act
            builder.Object.AddHttpCorrelation();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHttpCorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor<CorrelationInfo>>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<AzureFunctionsInProcessHttpCorrelation>());
            Assert.Null(provider.GetService<HttpCorrelation>());
        }

        [Fact]
        public void AddHttpCorrelation_WithoutOptions_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(build => build.Services).Returns(services);

            // Act
            builder.Object.AddHttpCorrelation((Action<HttpCorrelationInfoOptions>)null);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHttpCorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor<CorrelationInfo>>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<AzureFunctionsInProcessHttpCorrelation>());
            Assert.Null(provider.GetService<HttpCorrelation>());
        }

        [Fact]
        public void AddHttpCorrelation_WithHttpOptions_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(build => build.Services).Returns(services);

            // Act
            builder.Object.AddHttpCorrelation(options => options.UpstreamService.ExtractFromRequest = true);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHttpCorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor<CorrelationInfo>>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<AzureFunctionsInProcessHttpCorrelation>());
            Assert.Null(provider.GetService<HttpCorrelation>());
        }

        [Fact]
        public void AddHttpCorrelation_WithHierarchical_StillRegistersHttpCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsHostBuilder>();
            builder.Setup(build => build.Services).Returns(services);
            
            // Act
            builder.Object.AddHttpCorrelation(options => options.Format = HttpCorrelationFormat.Hierarchical);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<HttpCorrelation>());
            Assert.Null(provider.GetService<AzureFunctionsInProcessHttpCorrelation>());
        }
    } 
}
