using Arcus.WebApi.Logging.Core.Correlation;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class HttpCorrelationOptionsTests
    {
        [Fact]
        public void SendOptions_AndReceiveOptions_AreSame()
        {
            // Arrange
            var sendOptions = new HttpCorrelationClientOptions();
            var receiveOptions = new HttpCorrelationInfoOptions();

            // Act
            Assert.Equal(sendOptions.TransactionIdHeaderName, receiveOptions.Transaction.HeaderName);
            Assert.Equal(sendOptions.UpstreamServiceHeaderName, receiveOptions.UpstreamService.HeaderName);
        }
    }
}
