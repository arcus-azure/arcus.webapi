using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddHttpCorrelation_WithDefault_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IHostingEnvironment>());

            // Act
            services.AddHttpCorrelation();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHttpCorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor<CorrelationInfo>>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<HttpCorrelation>());
        }

        [Fact]
        public void AddHttpCorrelation_WithOptions_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddHttpCorrelation((Action<CorrelationInfoOptions>) null);
#pragma warning restore CS0618 // Type or member is obsolete

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHttpCorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor<CorrelationInfo>>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<HttpCorrelation>());
        }
        
        [Fact]
        public void AddHttpCorrelation_WithHttpOptions_RegistersDedicatedCorrelation()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IHostingEnvironment>());

            // Act
            services.AddHttpCorrelation((Action<HttpCorrelationInfoOptions>) null);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHttpCorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor<CorrelationInfo>>());
            Assert.NotNull(provider.GetService<ICorrelationInfoAccessor>());
            Assert.NotNull(provider.GetService<HttpCorrelation>());
        }
    }
}
