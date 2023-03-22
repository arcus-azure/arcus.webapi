using Arcus.WebApi.Logging.Core.Correlation;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class HttpCorrelationInfoOptionsTests
    {
        [Fact]
        public void Options_SetHierarchicalFormat_ChangesDefaultUpstreamServiceHeader()
        {
            // Arrange
            var options = new HttpCorrelationInfoOptions();

            // Act
            options.Format = HttpCorrelationFormat.Hierarchical;

            // Assert
            Assert.Equal("Request-Id", options.UpstreamService.HeaderName);
        }

        [Fact]
        public void Options_Default_UsesW3CFormat()
        {
            // Arrange
            var options = new HttpCorrelationInfoOptions();

            // Act / Assert
            Assert.Equal(HttpCorrelationFormat.W3C, options.Format);
        }
    }
}
