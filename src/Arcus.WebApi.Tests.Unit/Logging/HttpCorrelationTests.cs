using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class HttpCorrelationTests
    {
        [Fact]
        public void Correlation_GetCorrelationInfo_UsesStubbedCorrelation()
        {
            // Arrange
            var expected = new CorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}");
            var correlationAccessor = new DefaultCorrelationInfoAccessor();
            correlationAccessor.SetCorrelationInfo(expected);

            IOptions<HttpCorrelationInfoOptions> options = Options.Create(new HttpCorrelationInfoOptions());
            IHttpContextAccessor contextAccessor = Mock.Of<IHttpContextAccessor>();
            ILogger<HttpCorrelation> logger = NullLogger<HttpCorrelation>.Instance;
            var correlation = new HttpCorrelation(options, contextAccessor, correlationAccessor, logger);

            // Act
            CorrelationInfo actual = correlation.GetCorrelationInfo();
            
            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void Correlation_SetCorrelationInfo_SetsNewCorrelation()
        {
            // Arrange
            var original = new CorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}");
            var correlationAccessor = new DefaultCorrelationInfoAccessor();
            correlationAccessor.SetCorrelationInfo(original);

            IOptions<HttpCorrelationInfoOptions> options = Options.Create(new HttpCorrelationInfoOptions());
            IHttpContextAccessor contextAccessor = Mock.Of<IHttpContextAccessor>();
            ILogger<HttpCorrelation> logger = NullLogger<HttpCorrelation>.Instance;
            var correlation = new HttpCorrelation(options, contextAccessor, correlationAccessor, logger);

            var expected = new CorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}");
            
            // Act
            correlation.SetCorrelationInfo(expected);
            
            // Assert
            CorrelationInfo actual = correlation.GetCorrelationInfo();
            Assert.NotSame(original, actual);
            Assert.Same(expected, actual);
        }
        
        [Fact]
        public void Create_WithoutOptions_Fails()
        {
            // Arrange
            ICorrelationInfoAccessor correlationAccessor = new DefaultCorrelationInfoAccessor();
            IHttpContextAccessor contextAccessor = Mock.Of<IHttpContextAccessor>();
            ILogger<HttpCorrelation> logger = NullLogger<HttpCorrelation>.Instance;
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new HttpCorrelation(options: null, httpContextAccessor: contextAccessor, correlationInfoAccessor: correlationAccessor, logger: logger));
        }

        [Fact]
        public void Create_WithoutHttpContextAccessor_Fails()
        {
            // Arrange
            IOptions<HttpCorrelationInfoOptions> options = Options.Create(new HttpCorrelationInfoOptions());
            ICorrelationInfoAccessor correlationAccessor = new DefaultCorrelationInfoAccessor();
            ILogger<HttpCorrelation> logger = NullLogger<HttpCorrelation>.Instance;
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new HttpCorrelation(options, httpContextAccessor: null, correlationInfoAccessor: correlationAccessor, logger: logger));
        }

        [Fact]
        public void Create_WithoutCorrelationInfoAccessor_Fails()
        {
            // Arrange
            IOptions<HttpCorrelationInfoOptions> options = Options.Create(new HttpCorrelationInfoOptions());
            IHttpContextAccessor contextAccessor = Mock.Of<IHttpContextAccessor>();
            ILogger<HttpCorrelation> logger = NullLogger<HttpCorrelation>.Instance;
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new HttpCorrelation(options, contextAccessor, correlationInfoAccessor: null, logger: logger));
        }
    }
}
