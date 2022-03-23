using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
#if NET6_0
using Microsoft.Azure.Functions.Extensions.DependencyInjection; 
#endif
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
#if NET6_0
    // ReSharper disable once InconsistentNaming
    public class IFunctionsHostBuilderTests
    {
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
        }
    } 
#endif
}
