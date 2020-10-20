using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Extensions
{
    // ReSharper disable once InconsistentNaming
    public class IServiceProviderExtensionsTests
    {
        [Fact]
        public void GetsLoggerOrDefault_WithoutLogger_GetsDefault()
        {
            // Arrange
            var services = new ServiceCollection();
            IServiceProvider provider = services.BuildServiceProvider();

            // Act
            ILogger logger = provider.GetLoggerOrDefault<IServiceProviderExtensionsTests>();

            // Assert
            Assert.IsType<NullLogger<IServiceProviderExtensionsTests>>(logger);
        }

        [Fact]
        public void GetsLoggerOrDefault_WithLogger_GetsLogger()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            IServiceProvider provider = services.BuildServiceProvider();

            // Act
            ILogger logger = provider.GetLoggerOrDefault<IServiceProviderExtensionsTests>();

            // Assert
            Assert.IsNotType<NullLogger<IServiceProviderExtensionsTests>>(logger);
        }
    }
}
