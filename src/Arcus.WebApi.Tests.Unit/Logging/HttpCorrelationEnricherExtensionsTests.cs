using System;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class HttpCorrelationEnricherExtensionsTests
    {
        [Fact]
        public void WithHttpCorrelationInfo_WithCorrelationAccessor_Succeeds()
        {
            // Arrange
            var config = new LoggerConfiguration();
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IHttpCorrelationInfoAccessor>());
            IServiceProvider provider = services.BuildServiceProvider();

            // Act
            config.Enrich.WithHttpCorrelationInfo(provider);

            // Assert
            Logger logger = config.CreateLogger();
            Assert.NotNull(logger);
        }

        [Fact]
        public void WithHttpCorrelationInfo_WithoutRegisteredCorrelationAccessor_Fails()
        {
            // Arrange
            var config = new LoggerConfiguration();
            var services = new ServiceCollection();
            IServiceProvider provider = services.BuildServiceProvider();

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => config.Enrich.WithHttpCorrelationInfo(provider));
        }

        [Fact]
        public void WithHttpCorrelationInfo_WithoutServiceProvider_Fails()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => config.Enrich.WithHttpCorrelationInfo(serviceProvider: null));
        }
    }
}
