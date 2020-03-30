using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Correlation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arcus.WebApi.Unit.Correlation
{
    public class IServiceCollectionTests
    {
        [Fact]
        public void AddCorrelation_MappedToCorrectCorrelationOptions_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddCorrelation((CorrelationOptions options) => { });

            // Act
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Assert
            var infoOptions = serviceProvider.GetService<IOptions<CorrelationInfoOptions>>();
            Assert.NotNull(infoOptions);
        }
    }
}
