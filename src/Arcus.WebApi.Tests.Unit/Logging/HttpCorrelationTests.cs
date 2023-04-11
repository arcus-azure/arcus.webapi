using System;
using System.Collections.Generic;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
using Arcus.WebApi.Tests.Core;
using Arcus.WebApi.Tests.Unit.Logging.Fixture;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();
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
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();
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
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();
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
            var correlationAccessor = new StubHttpCorrelationInfoAccessor();
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
