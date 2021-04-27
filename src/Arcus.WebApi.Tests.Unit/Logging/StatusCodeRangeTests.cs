using System;
using Arcus.WebApi.Logging;
using Bogus;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class StatusCodeRangeTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void CreateRange_WithSingleThresholdOutOfMinimumRange_Fails()
        {
            // Arrange
            int statusCode = BogusGenerator.Random.Int(max: 99);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new StatusCodeRange(statusCode));
        }
        
        [Fact]
        public void CreateRange_WithSingleThresholdOutOfMaximumRange_Fails()
        {
            // Arrange
            int statusCode = BogusGenerator.Random.Int(min: 600);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new StatusCodeRange(statusCode));
        }
        
        [Fact]
        public void CreateRange_WithMinimumThresholdOutOfRange_Fails()
        {
            // Arrange
            int minimum = BogusGenerator.Random.Int(max: 99);
            int maximum = BogusGenerator.Random.Int(100, 599);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new StatusCodeRange(minimum, maximum));
        }

        [Fact]
        public void CreateRange_WithMaximumThresholdOutOfRange_Fails()
        {
            // Arrange
            int minimum = BogusGenerator.Random.Int(100, 599);
            int maximum = BogusGenerator.Random.Int(min: 600);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new StatusCodeRange(minimum, maximum));
        }

        [Fact]
        public void CreateRange_WithExpectedSingleThreshold_IsWithinRangeSucceeds()
        {
            // Arrange
            int statusCode = BogusGenerator.Random.Int(100, 599);
            var range = new StatusCodeRange(statusCode);

            // Act
            bool isWithinRange = range.IsWithinRange(statusCode);
            
            // Assert
            Assert.True(isWithinRange);
        }
        
        [Fact]
        public void CreateRange_WithOutsideExpectedSingleThreshold_IsWithinRangeFails()
        {
            // Arrange
            int threshold = BogusGenerator.Random.Int(400, 599);
            int statusCode = BogusGenerator.Random.Int(200, 399);
            var range = new StatusCodeRange(threshold);

            // Act
            bool isWithinRange = range.IsWithinRange(statusCode);
            
            // Assert
            Assert.False(isWithinRange);
        }
        
        [Fact]
        public void CreateRange_WithExpectedThresholds_IsWithinRangeSucceeds()
        {
            // Arrange
            int statusCode = BogusGenerator.Random.Int(min: 500, max: 599);
            var range = new StatusCodeRange(500, 599);

            // Act
            bool isWithinRange = range.IsWithinRange(statusCode);
            
            // Assert
            Assert.True(isWithinRange);
        }

        [Fact]
        public void CreateRange_WithOutsideExpectedThresholds_IsWithinRangeFails()
        {
            // Arrange
            int statusCode = BogusGenerator.Random.Int(min: 200, max: 299);
            var range = new StatusCodeRange(500, 599);

            // Act
            bool isWithinRange = range.IsWithinRange(statusCode);
            
            // Assert
            Assert.False(isWithinRange);
        }
    }
}
