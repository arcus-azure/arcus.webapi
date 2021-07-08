﻿using System;
using System.Collections.Generic;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Logging.Correlation;
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
        [Theory]
        [InlineData("|", ".")]
        [InlineData("", ".")]
        [InlineData("|", "")]
        [InlineData("", "")]
        [InlineData("|", ".other-id.")]
        [InlineData("|", ".other-id")]
        public void TryCorrelate_WithCorrectOperationParentId_SetsOperationIds(string prefix, string postfix)
        {
            // Arrange
            var operationId = $"operation-{Guid.NewGuid()}";
            string operationParentId = prefix + operationId + postfix;
            var headers = new Dictionary<string, StringValues>
            {
                ["Request-Id"] = operationParentId
            };

            HttpContext context = CreateHttpContext(headers);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(accessor => accessor.HttpContext).Returns(context);
            var correlationAccessor = new DefaultCorrelationInfoAccessor();
            
            var options = Options.Create(new HttpCorrelationInfoOptions());
            var correlation = new HttpCorrelation(options, contextAccessor.Object, correlationAccessor, NullLogger<HttpCorrelation>.Instance);
            
            // Act
            bool isCorrelated = correlation.TryHttpCorrelate(out string errorMessage);
            
            // Assert
            Assert.True(isCorrelated, errorMessage);
            Assert.Null(errorMessage);
            var correlationInfo = context.Features.Get<CorrelationInfo>();
            Assert.Equal(operationId, correlationInfo.OperationId);
            Assert.Equal(operationParentId, correlationInfo.OperationParentId);
        }

        [Fact]
        public void TryCorrelate_WithCorrectOperationParentIdWithDisabledUpstreamExtraction_DoesntSetExpectedOperationId()
        {
            // Arrange
            var operationIdFromGeneration = $"operation-{Guid.NewGuid()}";
            var operationIdFromUpstream = $"operation-{Guid.NewGuid()}";
            string operationParentId = $"|{operationIdFromUpstream}.";
            var headers = new Dictionary<string, StringValues>
            {
                ["Request-Id"] = operationParentId
            };

            HttpContext context = CreateHttpContext(headers);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(accessor => accessor.HttpContext).Returns(context);
            var correlationAccessor = new DefaultCorrelationInfoAccessor();
            
            var options = Options.Create(new HttpCorrelationInfoOptions
            {
                Operation = { GenerateId = () => operationIdFromGeneration },
                UpstreamService = { ExtractFromRequest = false }
            });
            var correlation = new HttpCorrelation(options, contextAccessor.Object, correlationAccessor, NullLogger<HttpCorrelation>.Instance);
            
            // Act
            bool isCorrelated = correlation.TryHttpCorrelate(out string errorMessage);
            
            // Assert
            Assert.True(isCorrelated, errorMessage);
            Assert.Null(errorMessage);
            var correlationInfo = context.Features.Get<CorrelationInfo>();
            Assert.NotEqual(operationIdFromUpstream, correlationInfo.OperationId);
            Assert.Equal(operationIdFromGeneration, correlationInfo.OperationId);
            Assert.Null(correlationInfo.OperationParentId);
        }

        [Theory]
        [InlineData("||", "")]
        [InlineData("|", "..")]
        [InlineData(".", "|")]
        [InlineData("|", ".other-id..")]
        public void TryCorrelate_WithIncorrectOperationParentId_DoesntSetExpectedOperationId(string prefix, string postfix)
        {
            // Arrange
            var operationId = $"operation-{Guid.NewGuid()}";
            var headers = new Dictionary<string, StringValues>
            {
                ["Request-Id"] = prefix + operationId + postfix
            };
            
            HttpContext context = CreateHttpContext(headers);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(accessor => accessor.HttpContext).Returns(context);
            var correlationAccessor = new DefaultCorrelationInfoAccessor();
            
            var options = Options.Create(new HttpCorrelationInfoOptions());
            var correlation = new HttpCorrelation(options, contextAccessor.Object, correlationAccessor, NullLogger<HttpCorrelation>.Instance);
            
            // Act / Assert
            Assert.True(correlation.TryHttpCorrelate(out string errorMessage), errorMessage);
            Assert.Null(errorMessage);
            var correlationInfo = context.Features.Get<CorrelationInfo>();
            Assert.NotEqual(operationId, correlationInfo.OperationId);
            Assert.Null(correlationInfo.OperationParentId);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void TryCorrelate_WithBlankOperationParentId_DoesntSetOperationId(string operationParentId)
        {
            // Arrange
            var headers = new Dictionary<string, StringValues>
            {
                ["Request-Id"] = operationParentId
            };
            
            HttpContext context = CreateHttpContext(headers);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(accessor => accessor.HttpContext).Returns(context);
            var correlationAccessor = new DefaultCorrelationInfoAccessor();
            
            var options = Options.Create(new HttpCorrelationInfoOptions());
            var correlation = new HttpCorrelation(options, contextAccessor.Object, correlationAccessor, NullLogger<HttpCorrelation>.Instance);
            
            // Act / Assert
            Assert.True(correlation.TryHttpCorrelate(out string errorMessage), errorMessage);
            Assert.Null(errorMessage);
            var correlationInfo = context.Features.Get<CorrelationInfo>();
            Assert.NotNull(correlationInfo.OperationId);
            Assert.Null(correlationInfo.OperationParentId);
        }

        private static HttpContext CreateHttpContext(Dictionary<string, StringValues> requestHeaders)
        {
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Headers).Returns(new HeaderDictionary(requestHeaders));
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.Headers).Returns(new HeaderDictionary());
            var context = new Mock<HttpContext>();
            context.Setup(c => c.Request).Returns(request.Object);
            context.Setup(c => c.Response).Returns(response.Object);
            var features = new FeatureCollection();
            context.Setup(c => c.Features).Returns(features);

            return context.Object;
        }
        
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
