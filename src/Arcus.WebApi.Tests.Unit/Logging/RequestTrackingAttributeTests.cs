using System;
using System.Net;
using Arcus.WebApi.Logging;
using Bogus;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class RequestTrackingAttributeTests
    {
        private static readonly Faker BogusGenerator = new Faker();
        
        [Theory]
        [InlineData(Exclude.None)]
        [InlineData(Exclude.RequestBody | Exclude.ResponseBody)]
        [InlineData((Exclude) 5)]
        public void CreateAttribute_WithExclusionOutOfExpectedRange_Fails(Exclude filter)
        {
            Assert.ThrowsAny<ArgumentException>(() => new RequestTrackingAttribute(filter));
        }
        
        [Theory]
        [InlineData((HttpStatusCode) 40)]
        [InlineData((HttpStatusCode) 600)]
        public void CreateAttribute_WithHttpStatusCodeOutOfExpectedRange_Fails(HttpStatusCode statusCode)
        {
            Assert.ThrowsAny<ArgumentException>(() => new RequestTrackingAttribute(statusCode));
        }

        [Fact]
        public void CreateAttribute_WithMinimumStatusCodeOutOfExpectedRange_Fails()
        {
            // Arrange
            int minimumStatusCode = BogusGenerator.Random.Int(max: 99);
            int maximumStatusCode = BogusGenerator.Random.Int(100, 599);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => new RequestTrackingAttribute(minimumStatusCode, maximumStatusCode));
        }
        
        [Fact]
        public void CreateAttribute_WithMaximumStatusCodeOutOfExpectedRange_Fails()
        {
            // Arrange
            int minimumStatusCode = BogusGenerator.Random.Int(100, 599);
            int maximumStatusCode = BogusGenerator.Random.Int(min: 600);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => new RequestTrackingAttribute(minimumStatusCode, maximumStatusCode));
        }
    }
}
