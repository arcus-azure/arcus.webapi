using System;
using System.Net.Http;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    // ReSharper disable once InconsistentNaming
    public class IHttpClientBuilderExtensionsTests
    {
        [Fact]
        public void Add_WithHttpCorrelationInfoAccessor_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpCorrelation(opt => opt.Format = HttpCorrelationFormat.Hierarchical);
            IHttpClientBuilder builder = services.AddHttpClient("service-a");

            // Act
            builder.WithHttpCorrelationTracking();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            Assert.NotNull(factory.CreateClient("service-a"));
        }

        [Fact]
        public void Add_WithoutHttpCorrelationInfoAccessor_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            IHttpClientBuilder builder = services.AddHttpClient("service-a");

            // Act
            builder.WithHttpCorrelationTracking();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            Assert.Throws<InvalidOperationException>(() => factory.CreateClient("service-a"));
        }
    }
}
